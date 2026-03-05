using Sandbox;
using Editor;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace AnyEditor;

public static class AnyEditorMenu
{
	[Menu( "Editor", "Any Editor/Configure Path..." )]
	public static void ConfigurePath()
	{
		var fd = new FileDialog( null );
		fd.Title = "Select Editor Executable";
		fd.DefaultSuffix = ".exe";
		fd.SetNameFilter( "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*" );
		
		if ( fd.Execute() )
		{
			AnyEditorConfig.ExePath = fd.SelectedFile;
			// Default to just path when manually configuring
			AnyEditorConfig.Arguments = "\"{path}\""; 
			AnyEditorConfig.AddRecent( fd.SelectedFile );
			Log.Info( $"AnyEditor path set to: {fd.SelectedFile}" );
		}
	}

	// Because i'm lazy
	[Menu( "Editor", "Any Editor/Recent Editors..." )]
	public static void ShowRecents()
	{
		new RecentEditorsDialog();
	}

	// You can add more here if you want, up to you - or just use recents.
	// If path attribution is available, add it in to the -g args.
	[Menu( "Editor", "Any Editor/Common/Set to Cursor" )]
	public static void SetToCursor() => SetCommonEditor( "Cursor", 
		new[] { "Cursor/Cursor.exe", "cursor/cursor.exe" }, 
		new[] { "Cursor.exe" },
		"-g \"{path}:{line}:{column}\"" );

	[Menu( "Editor", "Any Editor/Common/Set to VS Code" )]
	public static void SetToVSCode() => SetCommonEditor( "VS Code", 
		new[] { "Microsoft VS Code/Code.exe" }, 
		new[] { "Code.exe", "VSCode.exe" },
		"-g \"{path}:{line}:{column}\"" );

	[Menu( "Editor", "Any Editor/Common/Set to Visual Studio 2026" )]
	public static void SetToVS2026() => SetCommonEditorWildcard( "Visual Studio 2026", "Microsoft Visual Studio", "2026", "*/Common7/IDE/devenv.exe", null, "\"{path}\"" );

	[Menu( "Editor", "Any Editor/Common/Set to Visual Studio 2025" )]
	public static void SetToVS2025() => SetCommonEditorWildcard( "Visual Studio 2025", "Microsoft Visual Studio", "2025", "*/Common7/IDE/devenv.exe", null, "\"{path}\"" );

	[Menu( "Editor", "Any Editor/Common/Set to Visual Studio 2022" )]
	public static void SetToVS2022() => SetCommonEditorWildcard( "Visual Studio 2022", "Microsoft Visual Studio", "2022", "*/Common7/IDE/devenv.exe", null, "\"{path}\"" );

	[Menu( "Editor", "Any Editor/Common/Set to Visual Studio 2019" )]
	public static void SetToVS2019() => SetCommonEditorWildcard( "Visual Studio 2019", "Microsoft Visual Studio", "2019", "*/Common7/IDE/devenv.exe", null, "\"{path}\"" );

	[Menu( "Editor", "Any Editor/Common/Set to Visual Studio (Latest)" )]
	public static void SetToVSLatest() => SetCommonEditorWildcard( "Visual Studio (Latest)", "Microsoft Visual Studio", "*", "*/Common7/IDE/devenv.exe", "devenv.exe", "\"{path}\"" );

	[Menu( "Editor", "Any Editor/Common/Set to Rider" )]
	public static void SetToRider() => SetCommonEditorWildcard( "Rider", "JetBrains", "JetBrains Rider*", "bin/rider64.exe", "rider64.exe", "\"{path}\" --line {line} --column {column}" );

	[Menu( "Editor", "Any Editor/Common/Set to Zed" )]
	public static void SetToZed() => SetCommonEditor( "Zed", 
		new[] { "Zed/zed.exe" }, 
		new[] { "zed.exe" },
		"\"{path}:{line}:{column}\"" );

	[Menu( "Editor", "Any Editor/Common/Set to Sublime Text" )]
	public static void SetToSublime() => SetCommonEditor( "Sublime Text", 
		new[] { "Sublime Text/sublime_text.exe", "Sublime Text 3/sublime_text.exe" }, 
		new[] { "sublime_text.exe" },
		"\"{path}:{line}:{column}\"" );

	[Menu( "Editor", "Any Editor/Common/Set to Notepad++" )]
	public static void SetToNotepadPlusPlus() => SetCommonEditor( "Notepad++", 
		new[] { "Notepad++/notepad++.exe" }, 
		new[] { "notepad++.exe" },
		"\"{path}\" -n{line} -c{column}" );

	[Menu( "Editor", "Any Editor/Common/Set to Notepad" )]
	public static void SetToNotepad() => SetCommonEditor( "Notepad", 
		new[] { "Windows/System32/notepad.exe", "Windows/notepad.exe" }, 
		new[] { "notepad.exe" },
		"\"{path}\"" );

	[Menu( "Editor", "Any Editor/Common/Set to Google Antigravity" )]
	public static void SetToAntigravity() => SetCommonEditor( "Google Antigravity", new string[] { }, new[] { "Antigravity.exe" }, "-g \"{path}:{line}:{column}\"" );


	[Menu( "Editor", "Any Editor/Information" )]
	public static void ShowInfo()
	{
		var msg = "Any Editor Library\n\n" +
				  "Allows you to use any executable as your code editor in S&Box.\n\n" +
				  "USAGE:\n" +
				  "1. Configure the path via 'Editor > Any Editor > Configure Path...' (or select a Common editor)\n" +
				  "2. Go to 'Edit > Preferences' and select 'Any Editor (Custom)' as your Code Editor.\n" +
				  "   Note: You MUST select 'Any Editor (Custom)' in preferences for this to work.\n\n" +
				  "UNINSTALLATION:\n" +
				  "If you remove this library, you should manually:\n" +
				  "1. Delete 'anyeditor.config.json' from your project root.\n" +
				  "2. Remove the 'anyeditor.config.json' entry from your .gitignore file.\n\n" +
				  $"Current Configured Path: {AnyEditorConfig.ExePath}\n" +
				  $"Current Arguments: {AnyEditorConfig.Arguments}\n" +
				  $"Configuration File: {AnyEditorConfig.GetConfigFileLocation()}\n\n" +
				  "TROUBLESHOOTING:\n" +
				  "If this library spontaneously combusts after a S&Box update,\n" +
				  "simply uninstall it and pretend we never met. No hard feelings.\n\n" +
				  "CREDITS:\n" +
				  "Laoh\n"+
				  "Pixel VG for the inspiration from Cursor Editor (https://sbox.game/kolpak/cursoreditor) - check them out if you just use cursor and dont want the bloat";
		
		EditorUtility.DisplayDialog( "Any Editor Information", msg );
	}

	// Set common editor with path attribution
	private static void SetCommonEditor( string name, string[] relativePaths, string[] registryNames = null, string args = "\"{path}\"" )
	{
		string path = null;

		if ( registryNames != null )
		{
			foreach ( var exeName in registryNames )
			{
				path = FindPathInRegistry( exeName );
				if ( !string.IsNullOrEmpty( path ) && File.Exists( path ) ) break;
			}
		}

		if ( string.IsNullOrEmpty( path ) )
		{
			foreach( var p in relativePaths )
			{
				path = FindEditorPath( p );
				if ( !string.IsNullOrEmpty( path ) ) break;
			}
		}

		if ( !string.IsNullOrEmpty( path ) )
		{
			AnyEditorConfig.ExePath = path;
			AnyEditorConfig.Arguments = args;
			AnyEditorConfig.AddRecent( path );
			Log.Info( $"AnyEditor path set to {name}: {path}" );
			EditorUtility.DisplayDialog( "Success", $"AnyEditor path set to {name}!\n\nRemember to select 'Any Editor (Custom)' in Edit > Preferences." );
		}
		else
		{
			Log.Warning( $"Could not find {name} - either default path is cooked or registry is incorrect, you can fix this by configuring the path manually via 'Editor > Any Editor > Configure Path...' or updating the path in AnyEditorMenu.cs" );
			EditorUtility.DisplayDialog( "Not Found", $"Could not find {name} installation automatically.\nPlease configure the path manually. See Console." );
		}
	}

	private static void SetCommonEditorWildcard( string name, string baseFolder, string dirPattern, string relativeFilePattern, string registryName = null, string args = "\"{path}\"" )
	{
		string path = null;

		if ( !string.IsNullOrEmpty( registryName ) )
		{
			path = FindPathInRegistry( registryName );
		}

		if ( string.IsNullOrEmpty( path ) || !File.Exists( path ) )
		{
			path = FindEditorPathWildcard( baseFolder, dirPattern, relativeFilePattern );
		}
		
		if ( !string.IsNullOrEmpty( path ) )
		{
			AnyEditorConfig.ExePath = path;
			AnyEditorConfig.Arguments = args;
			AnyEditorConfig.AddRecent( path );
			Log.Info( $"AnyEditor path set to {name}: {path}" );
			EditorUtility.DisplayDialog( "Success", $"AnyEditor path set to {name}!\n\nRemember to select 'Any Editor (Custom)' in Edit > Preferences." );
		}
		else
		{
			Log.Warning( $"Could not find {name} - either default path is cooked or registry is incorrect, you can fix this by configuring the path manually via 'Editor > Any Editor > Configure Path...' or updating the path in AnyEditorMenu.cs" );
			EditorUtility.DisplayDialog( "Not Found", $"Could not find {name} installation automatically.\nPlease configure the path manually. See Console." );
		}
	}

	private static string FindPathInRegistry( string exeName )
	{
		try
		{
			// Some apps register here, so hardcoding it is
			string keyPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{exeName}";
			
			using ( var key = Registry.LocalMachine.OpenSubKey( keyPath ) )
			{
				var val = key?.GetValue( "" ) as string;
				if ( !string.IsNullOrEmpty( val ) ) return val;
			}

			using ( var key = Registry.CurrentUser.OpenSubKey( keyPath ) )
			{
				var val = key?.GetValue( "" ) as string;
				if ( !string.IsNullOrEmpty( val ) ) return val;
			}
			
			// Fallback: Also in here, infact most likely in here
			string keyPathClasses = $@"Applications\{exeName}\shell\open\command";
			using ( var key = Registry.ClassesRoot.OpenSubKey( keyPathClasses ) )
			{
				var val = key?.GetValue( "" ) as string;
				if ( !string.IsNullOrEmpty( val ) )
				{
					var split = val.Split( '"' );
					foreach( var s in split )
					{
						if ( s.EndsWith( ".exe", StringComparison.OrdinalIgnoreCase ) && File.Exists( s ) )
						{
							return s;
						}
					}
					
					if ( File.Exists( val ) ) return val;
				}
			}
		}
		catch { }
		return null;
	}

	private static string FindEditorPath( string relativePath )
	{
		// Also check System directory for things like notepad
		var systemPath = Environment.GetFolderPath( Environment.SpecialFolder.System );
		if ( File.Exists( Path.Combine( systemPath, Path.GetFileName( relativePath ) ) ) )
			return Path.Combine( systemPath, Path.GetFileName( relativePath ) );
			
		var paths = new[] {
			System.Environment.GetFolderPath( System.Environment.SpecialFolder.LocalApplicationData ),
			System.Environment.GetFolderPath( System.Environment.SpecialFolder.ProgramFiles ),
			System.Environment.GetFolderPath( System.Environment.SpecialFolder.ProgramFilesX86 ),
			System.Environment.GetFolderPath( System.Environment.SpecialFolder.Windows )
		};

		foreach ( var root in paths )
		{
			var path = Path.Combine( root, relativePath );
			if ( File.Exists( path ) ) return path;
		}

		return null;
	}

	private static string FindEditorPathWildcard( string baseFolder, string dirPattern, string relativeFilePattern )
	{
		// Basically if registry check fucks up, we'll just try the common places
		var roots = new[] {
			System.Environment.GetFolderPath( System.Environment.SpecialFolder.LocalApplicationData ),
			System.Environment.GetFolderPath( System.Environment.SpecialFolder.ProgramFiles ),
			System.Environment.GetFolderPath( System.Environment.SpecialFolder.ProgramFilesX86 )
		};

		foreach ( var root in roots )
		{
			var basePath = Path.Combine( root, baseFolder );
			if ( !Directory.Exists( basePath ) ) continue;

			try
			{
				var dirs = Directory.GetDirectories( basePath, dirPattern );
				Array.Sort( dirs );
				Array.Reverse( dirs );

				foreach ( var dir in dirs )
				{
					if ( !relativeFilePattern.Contains( "*" ) )
					{
						var fullPath = Path.Combine( dir, relativeFilePattern );
						if ( File.Exists( fullPath ) ) return fullPath;
					}
					else
					{
						var subParts = relativeFilePattern.Split( '/' );
						if ( subParts[0] == "*" )
						{
							var subPattern = string.Join( Path.DirectorySeparatorChar, subParts.Skip( 1 ) );
							var subDirs = Directory.GetDirectories( dir );
							foreach ( var subDir in subDirs )
							{
								var finalPath = Path.Combine( subDir, subPattern );
								if ( File.Exists( finalPath ) ) return finalPath;
							}
						}
					}
				}
			}
			catch { }
		}

		return null;
	}
}
