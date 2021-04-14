namespace InternetId.Common.Text
{
    public class Html
    {
        /// <summary>
        /// Strip HTML from source.
        /// </summary>
        /// <remarks>
        /// License: The Code Project Open License (CPOL) http://www.codeproject.com/info/cpol10.aspx
        /// Source: https://www.codeproject.com/Articles/11902/Convert-HTML-to-Plain-Text-2
        /// </remarks>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string Strip(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            string plain;

            // Remove HTML Development formatting
            // Replace line breaks with space
            // because browsers inserts space
            plain = html.Replace("\r", " ");
            // Replace line breaks with space
            // because browsers inserts space
            plain = plain.Replace("\n", " ");
            // Remove step-formatting
            plain = plain.Replace("\t", string.Empty);
            // Remove repeating spaces because browsers ignore them
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                                                                  @"( )+", " ");

            // Remove the header (prepare first by clearing attributes)
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"<( )*head([^>])*>", "<head>",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"(<( )*(/)( )*head( )*>)", "</head>",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     "(<head>).*(</head>)", string.Empty,
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // remove all scripts (prepare first by clearing attributes)
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"<( )*script([^>])*>", "<script>",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"(<( )*(/)( )*script( )*>)", "</script>",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            //result = System.Text.RegularExpressions.Regex.Replace(result,
            //         @"(<script>)([^(<script>\.</script>)])*(</script>)",
            //         string.Empty,
            //         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"(<script>).*(</script>)", string.Empty,
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // remove all styles (prepare first by clearing attributes)
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"<( )*style([^>])*>", "<style>",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"(<( )*(/)( )*style( )*>)", "</style>",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     "(<style>).*(</style>)", string.Empty,
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // insert tabs in spaces of <td> tags
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"<( )*td([^>])*>", "\t",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // insert line breaks in places of <BR> and <LI> tags
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"<( )*br( )*>", "\r",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"<( )*li( )*>", "\r",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // insert line paragraphs (double line breaks) in place
            // if <P>, <DIV> and <TR> tags
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"<( )*div([^>])*>", "\r\r",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"<( )*tr([^>])*>", "\r\r",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"<( )*p([^>])*>", "\r\r",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Remove remaining tags like <a>, links, images,
            // comments etc - anything that's enclosed inside < >
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"<[^>]*>", string.Empty,
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // replace special characters:
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @" ", " ",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"&bull;", " * ",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"&lsaquo;", "<",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"&rsaquo;", ">",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"&trade;", "(tm)",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"&frasl;", "/",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"&lt;", "<",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"&gt;", ">",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"&copy;", "(c)",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"&reg;", "(r)",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            // Remove all others. More can be added, see
            // http://hotwired.lycos.com/webmonkey/reference/special_characters/
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     @"&(.{2,6});", string.Empty,
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // for testing
            //System.Text.RegularExpressions.Regex.Replace(result,
            //       this.txtRegex.Text,string.Empty,
            //       System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // make line breaking consistent
            plain = plain.Replace("\n", "\r");

            // Remove extra line breaks and tabs:
            // replace over 2 breaks with 2 and over 4 tabs with 4.
            // Prepare first to remove any whitespaces in between
            // the escaped characters and remove redundant tabs in between line breaks
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     "(\r)( )+(\r)", "\r\r",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     "(\t)( )+(\t)", "\t\t",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     "(\t)( )+(\r)", "\t\r",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     "(\r)( )+(\t)", "\r\t",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            // Remove redundant tabs
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     "(\r)(\t)+(\r)", "\r\r",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            // Remove multiple tabs following a line break with just one tab
            plain = System.Text.RegularExpressions.Regex.Replace(plain,
                     "(\r)(\t)+", "\r\t",
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            // Initial replacement target string for line breaks
            string breaks = "\r\r\r";
            // Initial replacement target string for tabs
            string tabs = "\t\t\t\t\t";
            for (int index = 0; index < plain.Length; index++)
            {
                plain = plain.Replace(breaks, "\r\r");
                plain = plain.Replace(tabs, "\t\t\t\t");
                breaks = breaks + "\r";
                tabs = tabs + "\t";
            }

            // That's it.
            return plain;
        }
    }
}
