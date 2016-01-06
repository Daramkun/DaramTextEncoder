using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Daramkun.DaramTextEncoder.Properties;
using TaskDialogInterop;

namespace Daramkun.DaramTextEncoder
{
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window
	{
		public static List<Encoding> Encodings { get; private set; }
		static MainWindow ()
		{
			Encodings = new List<Encoding> ();
			foreach ( var encoding in Encoding.GetEncodings () )
			{
				var enc = Encoding.GetEncoding ( encoding.Name );
				if ( Encodings.Contains ( enc ) ) continue;
				Encodings.Add ( enc );
			}
			Encodings.Sort ( ( enc1, enc2 ) =>
			{
				var enc1name = enc1.WebName.ToLower ();
				var enc2name = enc2.WebName.ToLower ();
				var enc1subs = enc1name.Substring ( 0, 3 );
				var enc2subs = enc2name.Substring ( 0, 3 );
				if ( enc1subs == "utf" && enc2subs != "utf" )
					return -1;
				else if ( enc1subs != "utf" && enc2subs == "utf" )
					return 1;
				return enc1.WebName.ToLower ().CompareTo ( enc2.WebName.ToLower () );
			} );
		}

		ObservableCollection<string> fileCollection;

		public MainWindow ()
		{
			InitializeComponent ();

			Version currentVersion = Assembly.GetEntryAssembly ().GetName ().Version;
			Title = string.Format ( "{0} - v{1}.{2}{3}0", Title,
				currentVersion.Major, currentVersion.Minor, currentVersion.Build );

			fileCollection = new ObservableCollection<string> ();
			listViewFiles.ItemsSource = fileCollection;
			
			comboBoxOriginal.ItemsSource = Encodings;
			comboBoxTarget.ItemsSource = Encodings;
		}

		protected override void OnClosed ( EventArgs e )
		{
			Settings.Default.Save ();
			base.OnClosed ( e );
		}

		#region Utilities
		public void AddItem ( string s )
		{
			if ( System.IO.File.Exists ( s ) )
				fileCollection.Add ( s );
			else
			{
				foreach ( string ss in System.IO.Directory.GetFiles ( s ) )
					AddItem ( ss );
			}
		}

		public static void SimpleErrorMessage ( string message )
		{
			TaskDialogOptions config = new TaskDialogOptions ();
			config.Owner = null;
			config.Title = "다람 텍스트 인코더";
			config.MainInstruction = "오류가 발생했습니다.";
			config.Content = message;
			config.MainIcon = VistaTaskDialogIcon.Error;
			config.CustomButtons = new [] { "확인(&O)" };
			TaskDialog.Show ( config );
		}

		public static void SimpleDoneMessage ( string message )
		{
			TaskDialogOptions config = new TaskDialogOptions ();
			config.Owner = null;
			config.Title = "다람 텍스트 인코더";
			config.MainInstruction = "작업이 완료되었습니다.";
			config.Content = message;
			config.MainIcon = VistaTaskDialogIcon.Information;
			config.CustomButtons = new [] { "확인(&O)" };
			TaskDialog.Show ( config );
		}
		#endregion

		private void ListView_DragEnter ( object sender, DragEventArgs e )
		{
			if ( e.Data.GetDataPresent ( DataFormats.FileDrop ) ) e.Effects = DragDropEffects.None;
		}

		private void ListView_Drop ( object sender, DragEventArgs e )
		{
			if ( e.Data.GetDataPresent ( DataFormats.FileDrop ) )
			{
				var temp = e.Data.GetData ( DataFormats.FileDrop ) as string [];
				foreach ( string s in from b in temp orderby b select b ) AddItem ( s );
			}
		}

		private void ListView_KeyUp ( object sender, KeyEventArgs e )
		{
			if ( e.Key == Key.Delete )
			{
				List<string> tempFileInfos = new List<string> ();
				foreach ( string fileInfo in listViewFiles.SelectedItems ) tempFileInfos.Add ( fileInfo );
				foreach ( string fileInfo in tempFileInfos ) fileCollection.Remove ( fileInfo );
			}
		}

		private void Button_OpenFiles_Click ( object sender, RoutedEventArgs e )
		{
			Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog ();
			openFileDialog.Title = "열기";
			openFileDialog.Multiselect = true;
			openFileDialog.Filter = "프로그램이 알고 있는 모든 텍스트 파일|*.txt;*.md;*.cs;*.c;*.cpp;*.java;*.rb;*.py;*.xml;*.html;*.php;*.jsp;*.htm;*.config;*.ini;*.lua;*.mm;*.h;*.hpp;*.m;*.js;*.css;*.yaml;*.json;*.vb;*.fx;*.shader;*.swift;*.h;*.hpp;*.xaml;*.cson;*.smi;*.ass;*.srt;*.vs;*.ps;*.hlsl;*.glsl" +
				"|모든 파일(*.*)|*.*";
			if ( openFileDialog.ShowDialog () == false ) return;

			foreach ( string s in from s in openFileDialog.FileNames orderby s select s )
				AddItem ( s );
		}

		private void Button_Clear_Click ( object sender, RoutedEventArgs e )
		{
			fileCollection.Clear ();
		}

		private void Button_Apply_Click ( object sender, RoutedEventArgs e )
		{
			if ( comboBoxOriginal.SelectedIndex == -1 || comboBoxTarget.SelectedIndex == -1 )
			{
				SimpleErrorMessage ( "인코딩을 선택해주세요." );
				return;
			}

			foreach ( string filename in fileCollection )
			{
				string dest = ( checkBoxOverwrite.IsChecked.Value ) ? filename :
					System.IO.Path.Combine ( System.IO.Path.GetDirectoryName ( filename ), System.IO.Path.GetFileNameWithoutExtension ( filename ) + "_" +
					( comboBoxTarget.SelectedItem as Encoding ).WebName + System.IO.Path.GetExtension ( filename ) );
				string original = File.ReadAllText ( filename, comboBoxOriginal.SelectedItem as Encoding );
				using ( Stream stream = File.Create ( dest ) )
				{
					if ( checkBoxBOM.IsChecked.Value )
					{
						switch ( ( comboBoxTarget.SelectedItem as Encoding ).WebName.ToLower () )
						{
							case "utf-8": stream.Write ( new byte [] { 0xEF, 0xBB, 0xBF }, 0, 3 ); break;
							case "utf-16": stream.Write ( new byte [] { 0xFF, 0xFE }, 0, 2 ); break;
							case "utf-32": stream.Write ( new byte [] { 0xFF, 0xFE, 0, 0 }, 0, 4 ); break;
							case "utf-16be": stream.Write ( new byte [] { 0xFE, 0xFF }, 0, 2 ); break;
							case "utf-32be": stream.Write ( new byte [] { 0, 0, 0xFE, 0xFF }, 0, 4 ); break;
						}
					}

					StreamWriter writer = new StreamWriter ( stream, comboBoxTarget.SelectedItem as Encoding );
					writer.Write ( original );
					writer.Flush ();
				}
			}

			SimpleDoneMessage ( "모든 파일에 대해 인코딩을 변환하였습니다." );
		}

		private void Button_FindCorrectEncoding_Click ( object sender, RoutedEventArgs e )
		{
			new FindCorrectEncodingWindow ().Show ();
		}

		private void Button_Information_Click ( object sender, RoutedEventArgs e )
		{
			Process.Start ( "https://github.com/Daramkun/DaramTextEncoder/blob/master/LICENSE" );
		}
	}
}
