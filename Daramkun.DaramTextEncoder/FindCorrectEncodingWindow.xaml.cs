using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace Daramkun.DaramTextEncoder
{
	/// <summary>
	/// FindCorrectEncodingWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class FindCorrectEncodingWindow : Window
	{
		public FindCorrectEncodingWindow ()
		{
			InitializeComponent ();
			comboBoxEncoding.ItemsSource = MainWindow.Encodings;
		}

		#region Encoding Check
		private static int CompareArray ( byte [] a, byte [] b )
		{
			int diff = 0;
			for ( int i = 0; i < a.Length; ++i )
				diff += ( a [ i ] != b [ i ] ) ? 1 : 0;
			return diff;
		}

		public static void Check ( Stream stream, out Encoding encoding, out int skipByte )
		{
			byte [] buffer;
			long currentPosition = stream.Position;

			skipByte = 4;
			buffer = new byte [ 4 ];
			stream.Read ( buffer, 0, 4 );
			stream.Position = currentPosition;

			if ( CompareArray ( buffer, new byte [] { 0, 0, 0xFE, 0xFF } ) == 0 ) { encoding = Encoding.GetEncoding ( "utf-32BE" ); return; }
			if ( CompareArray ( buffer, new byte [] { 0xFF, 0xFE, 0, 0 } ) == 0 ) { encoding = Encoding.GetEncoding ( "utf-32" ); return; }

			skipByte = 2;
			buffer = new byte [ 2 ];
			stream.Read ( buffer, 0, 2 );
			stream.Position = currentPosition;

			if ( CompareArray ( buffer, new byte [] { 0xFE, 0xFF } ) == 0 ) { encoding = Encoding.BigEndianUnicode; return; }
			if ( CompareArray ( buffer, new byte [] { 0xFF, 0xFE } ) == 0 ) { encoding = Encoding.Unicode; return; }

			skipByte = 3;
			buffer = new byte [ 3 ];
			stream.Read ( buffer, 0, 3 );
			stream.Position = currentPosition;

			if ( CompareArray ( buffer, new byte [] { 0xEF, 0xBB, 0xBF } ) == 0 ) { encoding = Encoding.UTF8; return; }

			skipByte = 0;
			encoding = Encoding.Default;
		}

		public static Encoding Check ( Stream stream, out int skipByte )
		{
			Encoding encoding;
			Check ( stream, out encoding, out skipByte );
			return encoding;
		}

		public static Encoding Check ( Stream stream )
		{
			Encoding encoding;
			int skipByte;
			Check ( stream, out encoding, out skipByte );
			return encoding;
		}
		#endregion

		private void TextBox_MouseUp ( object sender, MouseButtonEventArgs e )
		{
			OpenFileDialog ofd = new OpenFileDialog ();
			ofd.Filter = "모든 파일(*.*)|*.*|텍스트 파일(*.txt)|*.txt";
			if ( !ofd.ShowDialog ().Value ) return;
			textBoxFilename.Text = ofd.FileName;
			using ( Stream stream = File.Open ( ofd.FileName, FileMode.Open ) )
			{
				Encoding encoding = Check ( stream );
				if ( encoding != Encoding.Default )
					comboBoxEncoding.SelectedItem = encoding;
			}
			radioSelectFile.IsChecked = true;
		}

		struct Encode { public string EncodingMethod { get; set; } public string DecodingMethod { get; set; } public string Encoded { get; set; } }

		private bool ContainsKorean ( string text )
		{
			bool contains = false;
			foreach ( var ch in text )
			{
				if ( ch >= '\u1100' && ch <= '\u11FF' )
					contains = true;
				if ( ch >= '\u3130' && ch <= '\u318F' )
					contains = true;
				if ( ch >= '\uA960' && ch <= '\uA97F' )
					contains = true;
				if ( ch >= '\uAC00' && ch <= '\uD7AF' )
					contains = true;
				if ( ch >= '\uD7B0' && ch <= '\uD7FF' )
					contains = true;
			}
			return contains;
		}

		private bool ContainsHanja ( string text )
		{
			bool contains = false;
			foreach ( var ch in text )
			{
				if ( ch >= '\u2E80' && ch <= '\u2EFF' )
					contains = true;
				if ( ch >= '\u2F00' && ch <= '\u2FDF' )
					contains = true;
				if ( ch >= '\u3400' && ch <= '\u4DBF' )
					contains = true;
				if ( ch >= '\u4E00' && ch <= '\u9FFF' )
					contains = true;
				if ( ch >= '\uF900' && ch <= '\uFAFF' )
					contains = true;
				/*if ( ch >= '\u20000' && ch <= '\u2A6DF' )
					contains = true;
				if ( ch >= '\u2A700' && ch <= '\u2B73F' )
					contains = true;
				if ( ch >= '\u2B740' && ch <= '\u2B81F' )
					contains = true;
				if ( ch >= '\u2B820' && ch <= '\u2CEAF' )
					contains = true;
				if ( ch >= '\u2F800' && ch <= '\u2FA1F' )
					contains = true;*/
			}
			return contains;
		}

		private bool ContainsKana ( string text )
		{
			bool contains = false;
			foreach ( var ch in text )
			{
				if ( ch >= '\u3040' && ch <= '\u309F' )
					contains = true;
				if ( ch >= '\u30A0' && ch <= '\u30FF' )
					contains = true;
				if ( ch >= '\u31F0' && ch <= '\u31FF' )
					contains = true;
			}
			return contains;
		}

		private bool ContainsArabic ( string text )
		{
			bool contains = false;
			foreach ( var ch in text)
			{
				if ( ch >= '\u0600' && ch <= '\u06FF' )
					contains = true;
				if ( ch >= '\u0750' && ch <= '\u077F' )
					contains = true;
				if ( ch >= '\u08A0' && ch <= '\u08FF' )
					contains = true;
				if ( ch >= '\uFB50' && ch <= '\uFDFF' )
					contains = true;
				if ( ch >= '\uFE70' && ch <= '\uFEFF' )
					contains = true;
			}
			return contains;
		}

		private void Button_Click ( object sender, RoutedEventArgs e )
		{
			string data;
			if ( radioSelectFile.IsChecked.Value )
				data = File.ReadLines ( textBoxFilename.Text, comboBoxEncoding.SelectedItem as Encoding ).First ();
			else data = textBoxUserInput.Text;

			List<Encode> encodes = new List<Encode> ();
			foreach ( Encoding decoding in comboBoxEncoding.ItemsSource )
			{
				foreach ( Encoding encoding in comboBoxEncoding.ItemsSource )
				{
					Encode encode = new Encode () { DecodingMethod = decoding.WebName.ToUpper (), EncodingMethod = encoding.WebName.ToUpper () };
					encode.Encoded = encoding.GetString ( decoding.GetBytes ( data ) );
					
					if ( !filterAll.IsChecked.Value )
					{
						bool filtered = false;
						if ( filterKorean.IsChecked.Value && ContainsKorean ( encode.Encoded ) ) filtered = true;
						if ( filterHanja.IsChecked.Value && ContainsHanja ( encode.Encoded ) ) filtered = true;
						if ( filterKana.IsChecked.Value && ContainsKana ( encode.Encoded ) ) filtered = true;
						if ( filterArab.IsChecked.Value && ContainsArabic ( encode.Encoded ) ) filtered = true;

						if ( !filtered ) continue;
					}

					encodes.Add ( encode );
				}
			}
			listViewEncoded.ItemsSource = encodes;
		}
	}
}
