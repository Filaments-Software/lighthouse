using Sandbox;
using Editor;
using System.Linq;

namespace AnyEditor;

public class RecentEditorsDialog : Dialog
{
	public RecentEditorsDialog()
	{
		WindowTitle = "Recent Editors";
		Size = new Vector2( 500, 400 );
		
		var layout = Layout.Column();
		layout.Margin = 10;
		layout.Spacing = 5;
		
		var recents = AnyEditorConfig.RecentPaths;
		
		if ( recents == null || recents.Count == 0 )
		{
			layout.Add( new Label( "It's lonely here." ) );
		}
		else
		{
			foreach ( var path in recents )
			{
				bool exists = System.IO.File.Exists( path );
				var btn = new Button( path + (exists ? "" : " (Missing)") );
				btn.Enabled = exists;
				btn.Clicked += () =>
				{
					AnyEditorConfig.ExePath = path;
					AnyEditorConfig.AddRecent( path );
					Log.Info( $"AnyEditor path set to: {path}" );
					Close();
					EditorUtility.DisplayDialog( "Success", $"AnyEditor path set to:\n{path}\n\nRemember to select 'Any Editor (Custom)' in Preferences." );
				};
				layout.Add( btn );
			}
		}
		
		layout.AddStretchCell();
		
		var clearBtn = new Button( "Clear List" );
		clearBtn.Clicked += () =>
		{
			AnyEditorConfig.ClearRecents();
			Close();
			new RecentEditorsDialog();
		};
		layout.Add( clearBtn );
		
		var closeBtn = new Button( "Close" );
		closeBtn.Clicked += Close;
		layout.Add( closeBtn );
		
		Layout = layout;
		Show();
	}
}
