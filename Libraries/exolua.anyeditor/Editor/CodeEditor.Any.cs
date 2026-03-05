using System;
using System.IO;
using AnyEditor;

namespace Editor.CodeEditors;

[Title( "Any Editor (Custom)" )]
public class AnyEditor : ICodeEditor
{
    // Implementation of https://sbox.game/api/Editor.ICodeEditor
	public void OpenFile( string path, int? line, int? column )
	{
		var args = AnyEditorConfig.Arguments;
		
		args = args.Replace( "{path}", path );
		args = args.Replace( "{line}", line?.ToString() ?? "0" );
		args = args.Replace( "{column}", column?.ToString() ?? "0" );

		Launch( args );
	}

	public void OpenSolution()
	{
		Launch( $"\"{Environment.CurrentDirectory}\"" );
	}

	public void OpenAddon( Project addon )
	{
		var projectPath = (addon != null) ? addon.GetRootPath() : "";
		Launch( $"\"{projectPath}\"" );
	}

	public bool IsInstalled()
	{
		var path = AnyEditorConfig.ExePath;
		return !string.IsNullOrEmpty( path ) && File.Exists( path );
	}

	private void Launch( string arguments )
	{
		var exePath = AnyEditorConfig.ExePath;
		if ( string.IsNullOrEmpty( exePath ) ) return;

		var startInfo = new System.Diagnostics.ProcessStartInfo
		{
			FileName = exePath,
			Arguments = arguments,
			CreateNoWindow = true,
			UseShellExecute = false
		};

		System.Diagnostics.Process.Start( startInfo );
	}
}
