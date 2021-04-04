namespace InternetId.Common
{
    public class InternetIdOptions
    {
        public string Title { get; set; } = null!;
        public string EmailFromAddress { get; set; } = null!;
        public string EmailSubjectFormat { get; set; } = "{0}";
        public string EmailBodyFormat { get; set; } = "<div style=\"font-family:system-ui,-apple-system,'Segoe UI',Roboto,'Helvetica Neue',Arial,'Noto Sans','Liberation Sans',sans-serif,'Apple Color Emoji','Segoe UI Emoji','Segoe UI Symbol','Noto Color Emoji'\"><h1>{0}</h1>{1}</div>";
    }
}
