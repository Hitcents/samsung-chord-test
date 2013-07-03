using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GooglePlayTest
{
    [DataContract]
    public class TimeStampedMessage
    {
        [DataMember]
        public DateTime TimeStamp { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public bool ShouldEcho { get; set; }
        [DataMember]
        public string PlayerId { get; set; }
    }
}
