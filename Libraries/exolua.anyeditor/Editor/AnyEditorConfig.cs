using Sandbox;
using Editor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace AnyEditor;

public static class AnyEditorConfig
{
	private const string ConfigFileName = "anyeditor.config.json";

	public static string ExePath
	{
		get => LoadConfig().ExePath;
		set
		{
			var config = LoadConfig();
			config.ExePath = value;
			SaveConfig( config );
		}
	}

	public static string Arguments
	{
		get => LoadConfig().Arguments;
		set
		{
			var config = LoadConfig();
			config.Arguments = value;
			SaveConfig( config );
		}
	}
	
	public static List<string> RecentPaths
	{
		get => LoadConfig().RecentPaths ?? new List<string>();
	}

	public static void AddRecent( string path )
	{
		if ( string.IsNullOrEmpty( path ) ) return;
		
		var config = LoadConfig();
		if ( config.RecentPaths == null ) config.RecentPaths = new List<string>();
		
		config.RecentPaths.RemoveAll( p => p.ToLower() == path.ToLower() );
		
		config.RecentPaths.Insert( 0, path );
		
		if ( config.RecentPaths.Count > 10 )
			config.RecentPaths = config.RecentPaths.Take( 10 ).ToList();
			
		SaveConfig( config );
	}
	
	public static void ClearRecents()
	{
		var config = LoadConfig();
		config.RecentPaths = new List<string>();
		SaveConfig( config );
	}
	
	public static string GetConfigFileLocation()
	{
		if ( Project.Current != null )
		{
			var root = Project.Current.GetRootPath();
			if ( !string.IsNullOrEmpty( root ) )
			{
				return Path.Combine( root, ConfigFileName );
			}
		}

		return Path.Combine( GetProjectRoot(), ConfigFileName );
	}

	private class ConfigData
	{
		public string ExePath { get; set; } = "";
		public string Arguments { get; set; } = "\"{path}\"";
		public List<string> RecentPaths { get; set; } = new List<string>();
	}

	private static ConfigData LoadConfig()
	{
		var path = GetConfigFileLocation();
		
		if ( !File.Exists( path ) )
			return new ConfigData();

		try
		{
			return System.Text.Json.JsonSerializer.Deserialize<ConfigData>( File.ReadAllText( path ) ) ?? new ConfigData();
		}
		catch
		{
			return new ConfigData();
		}
	}

	private static void SaveConfig( ConfigData config )
	{
		var path = GetConfigFileLocation();
		File.WriteAllText( path, System.Text.Json.JsonSerializer.Serialize( config ) );
		
		UpdateGitIgnore( ConfigFileName );
	}
	
	private static void UpdateGitIgnore( string filename )
	{
		string root = null;
		if ( Project.Current != null ) root = Project.Current.GetRootPath();
		if ( string.IsNullOrEmpty( root ) ) root = GetProjectRoot();
		
		if ( string.IsNullOrEmpty( root ) ) return;
		
		var gitIgnorePath = Path.Combine( root, ".gitignore" );
		if ( !File.Exists( gitIgnorePath ) ) return;
		
		try
		{
			var lines = File.ReadAllLines( gitIgnorePath ).ToList();
			bool hasEntry = lines.Any( l => l.Trim() == filename );
			
			if ( !hasEntry )
			{
				lines.Add( "" );
				lines.Add( "# AnyEditor Config" );
				lines.Add( filename );
				File.WriteAllLines( gitIgnorePath, lines );
			}
		}
		catch { }
	}

	private static string GetProjectRoot()
	{
		var dir = System.Environment.CurrentDirectory;
		
		foreach ( var addon in EditorUtility.Projects.GetAll() )
		{
			if ( addon.Config.Ident == "base" || addon.Config.Ident == "minimal" || addon.Config.Ident == "rust" )
				continue;

			if ( addon.Active )
			{
				return addon.GetRootPath();
			}
		}

		return dir;
	}
}
