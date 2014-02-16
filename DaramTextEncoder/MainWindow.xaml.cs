using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;

namespace DaramTextEncoder
{
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow ()
		{
			InitializeComponent ();
			List<string> encodings = new List<string> ();
			foreach ( EncodingInfo info in Encoding.GetEncodings () )
				encodings.Add ( info.Name );
			encodings.Sort ();
			comboBoxOriginal.ItemsSource = comboBoxDestination.ItemsSource = encodings;
			comboBoxOriginal.SelectedItem = "euc-kr";
			comboBoxDestination.SelectedItem = "utf-8";
		}

		private void listBoxFiles_DragEnter ( object sender, DragEventArgs e )
		{
			if ( e.Data.GetDataPresent ( DataFormats.FileDrop ) )
			{
				e.Effects = DragDropEffects.None;
			}
		}

		private void listBoxFiles_Drop ( object sender, DragEventArgs e )
		{
			if ( e.Data.GetDataPresent ( DataFormats.FileDrop ) )
			{
				string [] temp = e.Data.GetData ( DataFormats.FileDrop ) as string [];
				var data = from b in temp orderby b select b;
				foreach ( string s in data )
					listBoxFiles.Items.Add ( s );
			}
		}

		private void listBoxFiles_KeyUp ( object sender, KeyEventArgs e )
		{
			if ( e.Key == Key.Delete )
			{
				List<string> tempFilenames = new List<string> ();
				foreach ( string filename in listBoxFiles.SelectedItems )
					tempFilenames.Add ( filename );
				foreach ( string filename in tempFilenames )
					listBoxFiles.Items.Remove ( filename );
			}
		}

		private void buttonBrowse_Click ( object sender, RoutedEventArgs e )
		{
			OpenFileDialog ofd = new OpenFileDialog ();
			ofd.CheckFileExists = true;
			ofd.CheckPathExists = true;
			ofd.Filter = 
				"텍스트 파일(*.txt;*.md;*.cs;*.c;*.cpp;*.java;*.rb;*.py;*.xml;*.html;*.php;*.jsp;*.htm;*.config;*.ini;*.lua;*.mm;*.h;*.hpp;*.m;*.js;*.css;*.yaml;*.json;*.vb;*.fx;*.shader)" + 
				"|*.txt;*.md;*.cs;*.c;*.cpp;*.java;*.rb;*.py;*.xml;*.html;*.php;*.jsp;*.htm;*.config;*.ini;*.lua;*.mm;*.h;*.hpp;*.m;*.js;*.css;*.yaml;*.json;*.vb;*.fx;*.shader" + 
				"|모든 파일(*.*)|*.*";
			ofd.Title = "파일 가져오기";
			ofd.Multiselect = true;

			bool? result = ofd.ShowDialog ();
			if ( result != null && result.Value == true )
			{
				if ( ofd.FileNames.Length == 1 ) textBoxFilename.Text = ofd.FileName;
				else
				{
					foreach ( string filename in ofd.FileNames )
					{
						if ( !listBoxFiles.Items.Contains ( filename ) )
							listBoxFiles.Items.Add ( filename );
					}
				}
			}
		}

		private void buttonAddToList_Click ( object sender, RoutedEventArgs e )
		{
			if ( File.Exists ( textBoxFilename.Text ) )
			{
				if ( !listBoxFiles.Items.Contains ( textBoxFilename.Text ) )
					listBoxFiles.Items.Add ( textBoxFilename.Text );
				textBoxFilename.Text = "";
			}
		}

		private void buttonProcess_Click ( object sender, RoutedEventArgs e )
		{
			List<string> filenames = new List<string> ();
			foreach ( string filename in listBoxFiles.Items )
				filenames.Add ( filename );
			if ( File.Exists ( textBoxFilename.Text ) )
			{
				if ( !filenames.Contains ( textBoxFilename.Text ) )
					filenames.Add ( textBoxFilename.Text );
			}

			foreach ( string filename in filenames )
			{
				string dest = ( checkBoxOverwrite.IsChecked.Value ) ? filename : 
					Path.Combine ( Path.GetDirectoryName ( filename ), Path.GetFileNameWithoutExtension ( filename ) + "_" +
					comboBoxDestination.SelectedItem as string + Path.GetExtension ( filename ) );
				string original = File.ReadAllText ( filename, Encoding.GetEncoding ( comboBoxOriginal.SelectedItem as string ) );
				using ( Stream stream = File.Create ( dest ) )
				{
					if ( checkBoxBOM.IsChecked.Value )
					{
						switch ( comboBoxDestination.SelectedItem as string )
						{
							case "utf-8": stream.Write ( new byte [] { 0xEF, 0xBB, 0xBF }, 0, 3 ); break;
							case "utf-16": stream.Write ( new byte [] { 0xFF, 0xFE }, 0, 2 ); break;
							case "utf-32": stream.Write ( new byte [] { 0xFF, 0xFE, 0, 0 }, 0, 4 ); break;
							case "utf-16BE": stream.Write ( new byte [] { 0xFE, 0xFF }, 0, 2 ); break;
							case "utf-32BE": stream.Write ( new byte [] { 0, 0, 0xFE, 0xFF }, 0, 4 ); break;
						}
					}

					StreamWriter writer = new StreamWriter ( stream, Encoding.GetEncoding ( comboBoxDestination.SelectedItem as string ) );
					writer.Write ( original );
					writer.Flush ();
				}
			}

			MessageBox.Show ( "모든 작업이 완료되었습니다." );
		}
	}
}
