using System.Text.Json;
using Steamworks;

namespace WorkshopUploader;

internal sealed record WorkshopConfig(uint AppId, ulong PublishedFileId, string ContentDir, string[] RequiredFiles);

internal static class Program
{
	private static int Main(string[] args)
	{
		if (args.Length == 0 || (args[0] == "upload" && args.Length != 2) || (args[0] != "upload" && args[0] != "check"))
		{
			Console.Error.WriteLine("Usage: WorkshopUploader check | upload <changenote.txt>   (run from the repository root)");
			return 2;
		}

		WorkshopConfig config = LoadConfig("workshop.json");
		string contentDir = Path.GetFullPath(config.ContentDir);
		string contentVersion = ValidateContent(config, contentDir);
		string? changeNote = args[0] == "upload" ? ReadChangeNote(args[1]) : null;

		// steam_api reads the app id from this variable when steam_appid.txt is absent.
		Environment.SetEnvironmentVariable("SteamAppId", config.AppId.ToString());
		Environment.SetEnvironmentVariable("SteamGameId", config.AppId.ToString());
		if (!SteamAPI.Init())
		{
			throw new InvalidOperationException("SteamAPI.Init failed: the Steam client must be running and logged in to the account that owns the item.");
		}
		try
		{
			PrintItemDetails(config);
			Console.WriteLine($"Content: {contentDir} (version {contentVersion})");
			foreach (string file in Directory.GetFiles(contentDir))
			{
				Console.WriteLine($"  {Path.GetFileName(file)}  {new FileInfo(file).Length} bytes");
			}
			if (changeNote != null)
			{
				Upload(config, contentDir, changeNote);
			}
		}
		finally
		{
			SteamAPI.Shutdown();
		}
		return 0;
	}

	private static WorkshopConfig LoadConfig(string path)
	{
		WorkshopConfig? config = JsonSerializer.Deserialize<WorkshopConfig>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		if (config == null || config.AppId == 0 || config.PublishedFileId == 0 || string.IsNullOrWhiteSpace(config.ContentDir) || config.RequiredFiles == null || config.RequiredFiles.Length == 0)
		{
			throw new InvalidDataException($"{path} must define appId, publishedFileId, contentDir and requiredFiles.");
		}
		return config;
	}

	/// <summary>Refuses to upload a missing, incomplete or stale build; returns the packed manifest version.</summary>
	private static string ValidateContent(WorkshopConfig config, string contentDir)
	{
		foreach (string file in config.RequiredFiles)
		{
			if (!File.Exists(Path.Combine(contentDir, file)))
			{
				throw new FileNotFoundException($"Packed mod is missing {file}. Run `dotnet build -c Release` first.", Path.Combine(contentDir, file));
			}
		}
		string packedVersion = ReadManifestVersion(Path.Combine(contentDir, "mod_manifest.json"));
		string sourceVersion = ReadManifestVersion("mod_manifest.json");
		if (packedVersion != sourceVersion)
		{
			throw new InvalidOperationException($"Packed mod is {packedVersion} but mod_manifest.json says {sourceVersion}. Rebuild before uploading.");
		}
		return packedVersion;
	}

	private static string ReadManifestVersion(string path)
	{
		using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
		return doc.RootElement.GetProperty("version").GetString() ?? throw new InvalidDataException($"{path} has a null version.");
	}

	private static string ReadChangeNote(string path)
	{
		string note = File.ReadAllText(path).Trim();
		if (note.Length == 0)
		{
			throw new InvalidDataException($"Change note {path} is empty.");
		}
		return note;
	}

	private static void PrintItemDetails(WorkshopConfig config)
	{
		UGCQueryHandle_t query = SteamUGC.CreateQueryUGCDetailsRequest(new[] { new PublishedFileId_t(config.PublishedFileId) }, 1);
		try
		{
			SteamUGCQueryCompleted_t completed = Await<SteamUGCQueryCompleted_t>(SteamUGC.SendQueryUGCRequest(query));
			if (completed.m_eResult != EResult.k_EResultOK || !SteamUGC.GetQueryUGCResult(query, 0, out SteamUGCDetails_t details) || details.m_eResult != EResult.k_EResultOK)
			{
				throw new InvalidOperationException($"Could not read workshop item {config.PublishedFileId}: {completed.m_eResult}.");
			}
			CSteamID me = SteamUser.GetSteamID();
			if (details.m_ulSteamIDOwner != me.m_SteamID)
			{
				throw new InvalidOperationException($"Workshop item {config.PublishedFileId} belongs to {details.m_ulSteamIDOwner}, not the logged-in account {me.m_SteamID}.");
			}
			Console.WriteLine($"Item {config.PublishedFileId}: \"{details.m_rgchTitle}\", visibility {details.m_eVisibility}, updated {DateTimeOffset.FromUnixTimeSeconds(details.m_rtimeUpdated):u}");
			uint ranges = SteamUGC.GetNumSupportedGameVersions(query, 0);
			Console.WriteLine(ranges == 0 ? "Supported game branches: all (no range set)" : $"Supported game branch ranges: {ranges}");
			for (uint i = 0; i < ranges; i++)
			{
				if (!SteamUGC.GetSupportedGameVersionData(query, 0, i, out string min, out string max, 999u))
				{
					throw new InvalidOperationException($"Could not read supported branch range {i}.");
				}
				Console.WriteLine($"  [{i}] min '{min}' max '{max}'");
			}
		}
		finally
		{
			SteamUGC.ReleaseQueryUGCRequest(query);
		}
	}

	private static void Upload(WorkshopConfig config, string contentDir, string changeNote)
	{
		UGCUpdateHandle_t update = SteamUGC.StartItemUpdate(new AppId_t(config.AppId), new PublishedFileId_t(config.PublishedFileId));
		if (!SteamUGC.SetItemContent(update, contentDir))
		{
			throw new InvalidOperationException($"SetItemContent rejected {contentDir}.");
		}
		SteamAPICall_t call = SteamUGC.SubmitItemUpdate(update, changeNote);
		SubmitItemUpdateResult_t result = Await<SubmitItemUpdateResult_t>(call, () =>
		{
			EItemUpdateStatus status = SteamUGC.GetItemUpdateProgress(update, out ulong done, out ulong total);
			return $"{status} {done}/{total}";
		});
		if (result.m_bUserNeedsToAcceptWorkshopLegalAgreement)
		{
			Console.WriteLine("Steam says the Workshop legal agreement must be accepted before the update becomes visible.");
		}
		if (result.m_eResult != EResult.k_EResultOK)
		{
			throw new InvalidOperationException($"SubmitItemUpdate failed: {result.m_eResult}.");
		}
		Console.WriteLine($"Uploaded. https://steamcommunity.com/sharedfiles/filedetails/?id={config.PublishedFileId}");
	}

	private static T Await<T>(SteamAPICall_t call, Func<string>? progress = null) where T : struct
	{
		T? result = null;
		bool ioFailure = false;
		using CallResult<T> callResult = CallResult<T>.Create((value, failed) =>
		{
			result = value;
			ioFailure = failed;
		});
		callResult.Set(call);
		string lastProgress = "";
		while (result == null)
		{
			SteamAPI.RunCallbacks();
			if (progress != null)
			{
				string current = progress();
				if (current != lastProgress)
				{
					Console.WriteLine($"  {current}");
					lastProgress = current;
				}
			}
			Thread.Sleep(100);
		}
		if (ioFailure)
		{
			throw new IOException($"Steam call for {typeof(T).Name} failed with an IO failure.");
		}
		return result.Value;
	}
}
