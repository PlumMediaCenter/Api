namespace PlumMediaCenter.Business.Metadata
{
    public class Image
    {
        public int? Id { get; set; }
        public string OriginalUrl
        {
            get
            {
                return _OriginalUrl ?? Url;
            }
            set
            {
                this._OriginalUrl = value;
            }
        }
        private string _OriginalUrl;
        public string Url { get; set; }
    }
}