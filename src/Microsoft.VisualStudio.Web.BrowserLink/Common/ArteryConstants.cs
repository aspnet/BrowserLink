namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// Well-known strings that are shared between the EurekaPackage in Visual Studio
    /// and the HttpModule running in ASP.NET.
    /// </summary>
    internal static class BrowserLinkConstants
    {
        /// <summary>
        /// Name of the index file when VS is elevated. This file can be accessed
        /// by any user, including the IIS user.
        /// </summary>
        public const string ElevatedIndexFileName = @"Global\PageInspector.Artery";

        /// <summary>
        /// Name of the index file when VS is not elevated. This file can only be
        /// accessed by the current user, i.e. not by IIS.
        /// </summary>
        public const string NonElevatedIndexFileName = "PageInspector.Artery";

        public static readonly string[] IndexFileNames = new string[] 
        {
            ElevatedIndexFileName,
            NonElevatedIndexFileName,
        };

        /// <summary>
        /// Suffix appended to the instance file name to get the name of the
        /// Request Signal, which is set to indicate that Artery should start.
        /// </summary>
        public const string RequestSignalSuffix = ".RequestSignal";

        /// <summary>
        /// Suffix appended to the instance file name to get the name of the 
        /// Ready Signal, which blocks ASP.NET until the Artery server is ready.
        /// </summary>
        public const string ReadySignalSuffix = ".ReadySignal";

        /// <summary>
        /// Request header sent from the Browser Link runtime to identify which
        /// page request the messages correspond to.
        /// </summary>
        public const string RequestIdHeaderName = "BrowserLink-RequestID";

        /// <summary>
        /// Request sent from the Browser Link runtime to identify whether it's
        /// a "http" one or a "https" one.
        /// </summary>
        public const string RequestScheme = "Scheme";

        /// <summary>
        /// The url of the host used for project match.
        /// </summary>
        public const string RequestHostUrl = "HostUrl";

        /// <summary>
        /// Suffix added to the end of the instance file name to identify a
        /// Version 2 instance file
        /// </summary>
        public const string Version2Suffix = ".v2";

        // Keys for data in the instance file (v2)
        public const string HostNameKey = "host-name";
        public const string FetchScriptVerbKey = "verb-fetch-script";
        public const string InjectScriptVerbKey = "verb-inject-script";
        public const string MappingDataVerbKey = "verb-mapping-data";
        public const string HttpPortKey = "http-port";
        public const string HttpsPortKey = "https-port";
        public const string ServerDataVerbKey = "verb-server-data";
        public const string ProjectDataKey = "project";

        /// <summary>
        /// Constants representing the type of mapping data to follow
        /// </summary>
        public enum MappingDataType
        {
            /// <summary>
            /// This value is never sent over the wire. It is used internally for data
            /// blocks that are unrecognized by the receiver.
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// Push a source context onto the stack. Data to follow includes:
            ///     SourceStartPosition
            ///     SourceLength
            ///     SourceFilePath
            ///     RenderedPosition
            ///     RenderedOutputIndex
            ///     IsLiteral
            /// </summary>
            BeginContext = 1,

            /// <summary>
            /// Pop a source context from the stack. Data to follow includes:
            ///     RenderedOutputIndex
            ///     RenderedPosition
            /// </summary>
            EndContext = 2,

            /// <summary>
            /// Define a rendered output. Data to follow includes:
            ///     RenderedOutputIndex
            ///     RenderedContent
            /// </summary>
            RenderedOutputDefinition = 3,

            /// <summary>
            /// Define a relationship between to rendered outputs. Data to follow includes:
            ///     ParentRenderedOutputIndex
            ///     ChildRenderedOutputindex
            ///     RelativeRenderedPosition
            /// </summary>
            RenderedOutputRelationship = 4,

            /// <summary>
            /// Mapping data is complete.
            /// </summary>
            EndOfData = 5,
        }

        public enum MappingDataValueType
        {
            UnknownValue = 0,
            Int32Value   = 1,
            BooleanValue = 2,
            StringValue  = 3,
        }

        public enum MappingDataValue
        {
            /// <summary>
            /// (Int32) The first character position of the range of the source file
            /// that is currently being rendered.
            /// </summary>
            SourceStartPosition = 1,

            /// <summary>
            /// (Int32) The length of the range of the source file that is currently
            /// being rendered.
            /// </summary>
            SourceLength = 2,

            /// <summary>
            /// (string) The path to the source file that is currently being rendered.
            /// </summary>
            SourceFilePath = 3,

            /// <summary>
            /// (Int32) The current position in the rendered output that is being written.
            /// </summary>
            RenderedPosition = 4,

            /// <summary>
            /// (Int32) The index of the rendered output that is being written.
            /// </summary>
            RenderedOutputIndex = 5,

            /// <summary>
            /// (string) The final content of a rendered output
            /// </summary>
            RenderedContent = 6,

            /// <summary>
            /// (Boolean) True if a source range is being exactly copied from the source
            /// file to the rendered output. This means character-by-character mapping
            /// can be done in this source range.
            /// </summary>
            IsLiteral = 7,


            /// <summary>
            /// (Int32) For a rendered output relationship, this is the index of the
            /// rendered output that will contain the other output.
            /// </summary>
            ParentRenderedOutputIndex = 11,

            /// <summary>
            /// (Int32) For a rendered output relationship, this is the index of the
            /// rendered output that is contained by the other output.
            /// </summary>
            ChildRenderedOutputIndex = 12,

            /// <summary>
            /// (Int32) For a rendered output relationship, this is the position where
            /// the parent rendered output will contain the child rendered output.
            /// </summary>
            RelativeRenderedPosition = 13,

            
            /// <summary>
            /// No more data values in this block.
            /// </summary>
            EndOfDataValues = -1
        }

        /// <summary>
        /// Preamble bytes that are returned from BrowserLinkFilterOwinModule to force
        /// headers to be returned early.
        /// </summary>
        public static readonly byte[] FilterPreamble = new byte[] { 0xFF };
    }
}