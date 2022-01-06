﻿/*
    VectSharp - A light library for C# vector graphics.
    Copyright (C) 2020-2022 Giorgio Bianchini

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, version 3.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program. If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VectSharp
{
    /// <summary>
    /// Represents a typeface with a specific size.
    /// </summary>
    public class Font
    {
        /// <summary>
        /// Represents detailed information about the metrics of a text string when drawn with a certain font.
        /// </summary>
        public class DetailedFontMetrics
        {
            /// <summary>
            /// Width of the text (measured on the actual glyph outlines).
            /// </summary>
            public double Width { get; }

            /// <summary>
            /// Height of the text (measured on the actual glyph outlines).
            /// </summary>
            public double Height { get; }

            /// <summary>
            /// How much the leftmost glyph in the string overhangs the glyph origin on the left. Positive for glyphs that hang past the origin (e.g. italic 'f').
            /// </summary>
            public double LeftSideBearing { get; }

            /// <summary>
            /// How much the rightmost glyph in the string overhangs the glyph end on the right. Positive for glyphs that hang past the end (e.g. italic 'f').
            /// </summary>
            public double RightSideBearing { get; }

            /// <summary>
            /// Height of the tallest glyph in the string over the baseline. Always &gt;= 0.
            /// </summary>
            public double Top { get; }

            /// <summary>
            /// Depth of the deepest glyph in the string below the baseline. Always &lt;= 0.
            /// </summary>
            public double Bottom { get; }

            internal DetailedFontMetrics(double width, double height, double leftSideBearing, double rightSideBearing, double top, double bottom)
            {
                this.Width = width;
                this.Height = height;
                this.LeftSideBearing = leftSideBearing;
                this.RightSideBearing = rightSideBearing;
                this.Top = top;
                this.Bottom = bottom;
            }
        }

        /// <summary>
        /// Font size, in graphics units.
        /// </summary>
        public double FontSize { get; }

        /// <summary>
        /// Font typeface.
        /// </summary>
        public FontFamily FontFamily { get; }

        /// <summary>
        /// Create a new Font object, given the base typeface and the font size.
        /// </summary>
        /// <param name="fontFamily">Base typeface. See <see cref="FontFamily"/>.</param>
        /// <param name="fontSize">The font size, in graphics units.</param>
        public Font(FontFamily fontFamily, double fontSize)
        {
            this.FontFamily = fontFamily;
            this.FontSize = fontSize;
        }

        /// <summary>
        /// Maximum height over the baseline of the usual glyphs in the font (there may be glyphs taller than this). Always &gt;= 0.
        /// </summary>
        public double Ascent
        {
            get
            {
                if (this.FontFamily.TrueTypeFile == null)
                {
                    return 0;
                }
                else
                {
                    return this.FontFamily.TrueTypeFile.Get1000EmAscent() * this.FontSize / 1000;
                }
            }
        }

        /// <summary>
        /// Height above the baseline for a clipping region (Windows ascent). Always &gt;= 0.
        /// </summary>
        public double WinAscent
        {
            get
            {
                if (this.FontFamily.TrueTypeFile == null)
                {
                    return 0;
                }
                else
                {
                    return this.FontFamily.TrueTypeFile.Get1000EmWinAscent() * this.FontSize / 1000;
                }
            }
        }

        /// <summary>
        /// Maximum depth below the baseline of the usual glyphs in the font (there may be glyphs deeper than this). Always &lt;= 0.
        /// </summary>
        public double Descent
        {
            get
            {
                if (this.FontFamily.TrueTypeFile == null)
                {
                    return 0;
                }
                else
                {
                    return this.FontFamily.TrueTypeFile.Get1000EmDescent() * this.FontSize / 1000;
                }
            }
        }

        /// <summary>
        /// Absolute maximum height over the baseline of the glyphs in the font. Always &gt;= 0.
        /// </summary>
        public double YMax
        {
            get
            {
                if (this.FontFamily.TrueTypeFile == null)
                {
                    return 0;
                }
                else
                {
                    return this.FontFamily.TrueTypeFile.Get1000EmYMax() * this.FontSize / 1000;
                }
            }
        }

        /// <summary>
        /// Absolute maximum depth below the baseline of the glyphs in the font. Always &lt;= 0.
        /// </summary>
        public double YMin
        {
            get
            {
                if (this.FontFamily.TrueTypeFile == null)
                {
                    return 0;
                }
                else
                {
                    return this.FontFamily.TrueTypeFile.Get1000EmYMin() * this.FontSize / 1000;
                }
            }
        }

        /// <summary>
        /// Measure the size of a text string when typeset with this font.
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <returns>A <see cref="Size"/> object representing the width and height of the text.</returns>
        public Size MeasureText(string text)
        {
            if (this.FontFamily.TrueTypeFile != null)
            {
                double width = 0;
                double yMin = 0;
                double yMax = 0;

                for (int i = 0; i < text.Length; i++)
                {
                    width += this.FontFamily.TrueTypeFile.Get1000EmGlyphWidth(text[i]) * this.FontSize / 1000;
                    TrueTypeFile.VerticalMetrics vMet = this.FontFamily.TrueTypeFile.Get1000EmGlyphVerticalMetrics(text[i]);

                    yMin = Math.Min(yMin, vMet.YMin * this.FontSize / 1000);
                    yMax = Math.Max(yMax, vMet.YMax * this.FontSize / 1000);
                }

                width -= this.FontFamily.TrueTypeFile.Get1000EmGlyphBearings(text[0]).LeftSideBearing * this.FontSize / 1000;
                width -= this.FontFamily.TrueTypeFile.Get1000EmGlyphBearings(text[text.Length - 1]).RightSideBearing * this.FontSize / 1000;

                return new Size(width, yMax - yMin);
            }
            else
            {
                return new Size(0, 0);
            }
        }

        /// <summary>
        /// Measure all the metrics of a text string when typeset with this font.
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <returns>A <see cref="DetailedFontMetrics"/> object representing the metrics of the text.</returns>
        public DetailedFontMetrics MeasureTextAdvanced(string text)
        {
            if (this.FontFamily.TrueTypeFile != null)
            {
                double width = 0;
                double yMin = 0;
                double yMax = 0;

                for (int i = 0; i < text.Length; i++)
                {
                    width += this.FontFamily.TrueTypeFile.Get1000EmGlyphWidth(text[i]) * this.FontSize / 1000;
                    TrueTypeFile.VerticalMetrics vMet = this.FontFamily.TrueTypeFile.Get1000EmGlyphVerticalMetrics(text[i]);

                    yMin = Math.Min(yMin, vMet.YMin * this.FontSize / 1000);
                    yMax = Math.Max(yMax, vMet.YMax * this.FontSize / 1000);
                }

                double lsb = this.FontFamily.TrueTypeFile.Get1000EmGlyphBearings(text[0]).LeftSideBearing * this.FontSize / 1000;
                double rsb = this.FontFamily.TrueTypeFile.Get1000EmGlyphBearings(text[text.Length - 1]).RightSideBearing * this.FontSize / 1000;

                width -= lsb;
                width -= rsb;

                return new DetailedFontMetrics(width, yMax - yMin, lsb, rsb, yMax, yMin);
            }
            else
            {
                return new DetailedFontMetrics(0, 0, 0, 0, 0, 0);
            }
        }
    }


    /// <summary>
    /// Represents a typeface.
    /// </summary>
    public class FontFamily
    {
        /// <summary>
        /// The default font library used to resolve font family names.
        /// </summary>
        public static IFontLibrary DefaultFontLibrary { get; set; } = new DefaultFontLibrary();

        /// <summary>
        /// Create a new font family from the specified family name or true type file. If the family name or the true type file are not valid, an exception might be raised. Equivalent to DefaultFontLibrary.ResolveFontFamily.
        /// </summary>
        /// <param name="fontFamily">The name of the font family to create, or the path to a TTF file.</param>
        /// <returns>If the font family name or the true type file is valid, a <see cref="FontFamily"/> object corresponding to the specified font family.</returns>
        public static FontFamily ResolveFontFamily(string fontFamily) => DefaultFontLibrary.ResolveFontFamily(fontFamily);


        /// <summary>
        /// Create a new font family from the specified standard font family name. Equivalent to DefaultFontLibrary.ResolveFontFamily.
        /// </summary>
        /// <param name="standardFontFamily">The standard name of the font family.</param>
        /// <returns>A <see cref="FontFamily"/> object corresponding to the specified font family.</returns>
        public static FontFamily ResolveFontFamily(StandardFontFamilies standardFontFamily) => DefaultFontLibrary.ResolveFontFamily(standardFontFamily);

        /// <summary>
        /// Create a new font family from the specified family name or true type file. If the family name or the true type file are not valid, try to instantiate the font family using
        /// the <paramref name="fallback"/>. If none of the fallback family names or true type files are valid, an exception might be raised. Equivalent to DefaultFontLibrary.ResolveFontFamily.
        /// </summary>
        /// <param name="fontFamily">The name of the font family to create, or the path to a TTF file.</param>
        /// <param name="fallback">Names of additional font families or TTF files, which will be tried if the first <paramref name="fontFamily"/> is not valid.</param>
        /// <returns>A <see cref="FontFamily"/> object corresponding to the first of the specified font families that is valid.</returns>
        public static FontFamily ResolveFontFamily(string fontFamily, params string[] fallback) => DefaultFontLibrary.ResolveFontFamily(fontFamily, fallback);

        /// <summary>
        /// Create a new font family from the specified family name or true type file. If the family name or the true type file are not valid, try to instantiate the font family using
        /// the <paramref name="fallback"/>. If none of the fallback family names or true type files are valid, instantiate a standard font family using the <paramref name="finalFallback"/>. Equivalent to DefaultFontLibrary.ResolveFontFamily.
        /// </summary>
        /// <param name="fontFamily">The name of the font family to create, or the path to a TTF file.</param>
        /// <param name="fallback">Names of additional font families or TTF files, which will be tried if the first <paramref name="fontFamily"/> is not valid.</param>
        /// <param name="finalFallback">The standard name of the font family that will be used if none of the fallback families are valid.</param>
        /// <returns>A <see cref="FontFamily"/> object corresponding to the first of the specified font families that is valid.</returns>
        public static FontFamily ResolveFontFamily(string fontFamily, StandardFontFamilies finalFallback, params string[] fallback) => DefaultFontLibrary.ResolveFontFamily(fontFamily, finalFallback, fallback);

        internal static object fontFamilyLock = new object();
        internal static readonly Dictionary<string, Stream> manifestResources = new Dictionary<string, Stream>();

        internal static Stream GetManifestResourceStream(string name)
        {
            if (!manifestResources.ContainsKey(name))
            {
                manifestResources.Add(name, typeof(FontFamily).Assembly.GetManifestResourceStream(name));
            }

            return manifestResources[name];
        }


        /// <summary>
        /// The names of the 14 standard families that are guaranteed to be displayed correctly.
        /// </summary>
        public static string[] StandardFamilies = new string[] { "Times-Roman", "Times-Bold", "Times-Italic", "Times-BoldItalic", "Helvetica", "Helvetica-Bold", "Helvetica-Oblique", "Helvetica-BoldOblique", "Courier", "Courier-Bold", "Courier-Oblique", "Courier-BoldOblique", "Symbol", "ZapfDingbats" };

        /// <summary>
        /// The names of the resource streams pointing to the included TrueType font files for each of the standard 14 font families.
        /// </summary>
        public static string[] StandardFontFamilyResources = new string[]
        {
            "VectSharp.StandardFonts.Tinos-Regular.ttf", "VectSharp.StandardFonts.Tinos-Bold.ttf", "VectSharp.StandardFonts.Tinos-Italic.ttf", "VectSharp.StandardFonts.Tinos-BoldItalic.ttf",
            "VectSharp.StandardFonts.Arimo-Regular.ttf", "VectSharp.StandardFonts.Arimo-Bold.ttf",  "VectSharp.StandardFonts.Arimo-Italic.ttf", "VectSharp.StandardFonts.Arimo-BoldItalic.ttf",
            "VectSharp.StandardFonts.Cousine-Regular.ttf", "VectSharp.StandardFonts.Cousine-Bold.ttf", "VectSharp.StandardFonts.Cousine-Italic.ttf", "VectSharp.StandardFonts.Cousine-BoldItalic.ttf",
            "VectSharp.StandardFonts.SymbolNeu_GB.ttf", "VectSharp.StandardFonts.Levibats-Regular_GB.ttf"
        };

        /// <summary>
        /// Whether this is one of the 14 standard font families or not.
        /// </summary>
        public bool IsStandardFamily { get; internal set; }

        /// <summary>
        /// The 14 standard font families.
        /// </summary>
        public enum StandardFontFamilies
        {
            /// <summary>
            /// Serif normal regular face.
            /// </summary>
            TimesRoman,

            /// <summary>
            /// Serif bold regular face.
            /// </summary>
            TimesBold,

            /// <summary>
            /// Serif normal italic face.
            /// </summary>
            TimesItalic,

            /// <summary>
            /// Serif bold italic face.
            /// </summary>
            TimesBoldItalic,

            /// <summary>
            /// Sans-serif normal regular face.
            /// </summary>
            Helvetica,

            /// <summary>
            /// Sans-serif bold regular face.
            /// </summary>
            HelveticaBold,

            /// <summary>
            /// Sans-serif normal oblique face.
            /// </summary>
            HelveticaOblique,

            /// <summary>
            /// Sans-serif bold oblique face.
            /// </summary>
            HelveticaBoldOblique,

            /// <summary>
            /// Monospace normal regular face.
            /// </summary>
            Courier,

            /// <summary>
            /// Monospace bold regular face.
            /// </summary>
            CourierBold,

            /// <summary>
            /// Monospace normal oblique face.
            /// </summary>
            CourierOblique,

            /// <summary>
            /// Monospace bold oblique face.
            /// </summary>
            CourierBoldOblique,

            /// <summary>
            /// Symbol font.
            /// </summary>
            Symbol,

            /// <summary>
            /// Dingbat font.
            /// </summary>
            ZapfDingbats
        }

        /// <summary>
        /// Full path to the TrueType font file for this font family (or, if this is a standard font family, name of the font family).
        /// </summary>
        public string FileName { get; internal set; }

        /// <summary>
        /// Parsed TrueType font file for this font family.
        /// See also: <seealso cref="VectSharp.TrueTypeFile"/>.
        /// </summary>
        public TrueTypeFile TrueTypeFile { get; }

        /// <summary>
        /// Whether this font is bold or not. This is set based on the information included in the OS/2 table of the TrueType file.
        /// </summary>
        public bool IsBold { get; internal set; }

        /// <summary>
        /// Whether this font is italic or oblique or not. This is set based on the information included in the OS/2 table of the TrueType file.
        /// </summary>
        public bool IsItalic { get; internal set; }

        /// <summary>
        /// Whether this font is oblique or not. This is set based on the information included in the OS/2 table of the TrueType file.
        /// </summary>
        public bool IsOblique { get; internal set; }

        /// <summary>
        /// Create a new <see cref="FontFamily"/>.
        /// </summary>
        /// <param name="fileName">The full path to the TrueType font file for this font family or the name of a standard font family.</param>
        [Obsolete("Please use the FontFamily.ResolveFontFamily(string) method instead!", true)]
        public FontFamily(string fileName)
        {
            lock (fontFamilyLock)
            {
                FontFamily resolved = DefaultFontLibrary.ResolveFontFamily(fileName);

                this.FileName = resolved.FileName;
                this.TrueTypeFile = resolved.TrueTypeFile;
                this.IsOblique = resolved.IsOblique;
                this.IsBold = resolved.IsBold;
                this.IsItalic = resolved.IsItalic;
                this.IsStandardFamily = resolved.IsStandardFamily;
            }
        }

        internal FontFamily()
        {

        }

        /// <summary>
        /// Create a new <see cref="FontFamily"/>.
        /// </summary>
        /// <param name="ttfStream">A stream containing a file in TTF format.</param>
        public FontFamily(Stream ttfStream)
        {
            lock (fontFamilyLock)
            {
                IsStandardFamily = false;

                TrueTypeFile = TrueTypeFile.CreateTrueTypeFile(ttfStream);

                FileName = TrueTypeFile.GetFontFamilyName();
                this.IsBold = TrueTypeFile.IsBold();
                this.IsItalic = TrueTypeFile.IsItalic();
                this.IsOblique = TrueTypeFile.IsOblique();
            }
        }

        /// <summary>
        /// Create a new <see cref="FontFamily"/>.
        /// </summary>
        /// <param name="ttf">A font file in TTF format.</param>
        public FontFamily(TrueTypeFile ttf)
        {
            lock (fontFamilyLock)
            {
                IsStandardFamily = false;

                TrueTypeFile = ttf;

                FileName = TrueTypeFile.GetFontFamilyName();
                this.IsBold = TrueTypeFile.IsBold();
                this.IsItalic = TrueTypeFile.IsItalic();
                this.IsOblique = TrueTypeFile.IsOblique();
            }
        }

        /// <summary>
        /// Create a new standard <see cref="FontFamily"/>.
        /// </summary>
        /// <param name="standardFontFamily">The standard font family.</param>
        [Obsolete("Please use the FontFamily.ResolveFontFamily(StandardFontFamilies) method instead!", true)]
        public FontFamily(StandardFontFamilies standardFontFamily)
        {
            lock (fontFamilyLock)
            {
                FontFamily resolved = DefaultFontLibrary.ResolveFontFamily(standardFontFamily);

                this.FileName = resolved.FileName;
                this.TrueTypeFile = resolved.TrueTypeFile;
                this.IsOblique = resolved.IsOblique;
                this.IsBold = resolved.IsBold;
                this.IsItalic = resolved.IsItalic;
                this.IsStandardFamily = resolved.IsStandardFamily;
            }
        }
    }

    /// <summary>
    /// Represents a FontFamily created from a resource stream.
    /// </summary>
    public class ResourceFontFamily : FontFamily
    {
        /// <summary>
        /// The name of the embedded resource, which will be parsed using <code>Avalonia.Media.FontFamily.Parse(string, Uri)</code>.
        /// </summary>
        public string ResourceName;

        /// <summary>
        /// Create a new <see cref="ResourceFontFamily"/> from the specified <paramref name="resourceStream"/> containing a TTF file, passing the specified <paramref name="resourceName"/> to the <code>Avalonia.Media.FontFamily.Parse(string, Uri)"</code> method.
        /// </summary>
        /// <param name="resourceStream">A resource stream containing a TTF file.</param>
        /// <param name="resourceName">The name of the embedded resource, which will be parsed using <code>Avalonia.Media.FontFamily.Parse(string, Uri)</code>.</param>
        public ResourceFontFamily(System.IO.Stream resourceStream, string resourceName) : base(resourceStream)
        {
            this.ResourceName = resourceName;
        }
    }

}