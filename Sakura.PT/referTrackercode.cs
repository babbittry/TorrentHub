namespace WebApplication8.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class announceController : ControllerBase
    {
        private TrackerContext _db;
        public announceController(TrackerContext db)
        {
            _db = db;
        }
        // GET api/values
        [HttpGet]
        public string Get()
        {
            try
            {
                //?info_hash=o%b8t%7c~%e86%fc2%878%5c%f5%fbj0%40%26-a&peer_id=-UT354S-%e8%ad%86%f5%0d%ee%86%40%9aXo%f9&port=53974&uploaded=0&downloaded=0&left=0&corrupt=0&key=E96680BC&event=started&numwant=200&compact=1&no_peer_id=1
                var dic = GetDic(Request.QueryString.ToString());
                var infoHash = BitConverter.ToString(HttpUtility.UrlDecodeToBytes(dic["@info_hash"].ToString())).Replace("-", "").ToLower();
                var peer_id = BitConverter.ToString(HttpUtility.UrlDecodeToBytes(dic["@peer_id"].ToString())).Replace("-", "").ToLower();

                //判断是否存在该tracker
                var entity = _db.Bittorrents.FirstOrDefault(p => p.InfoHash == infoHash && p.PeerId == peer_id);
                //不存在插入tracker信息
                if (entity == null)
                {
                    _db.Bittorrents.Add(new Tracker
                    {
                        InfoHash = infoHash,
                        Ip = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                        Left = Convert.ToInt32(dic["@left"]),
                        Uploaded = Convert.ToInt32(dic["@uploaded"]),
                        Downloaded = Convert.ToInt32(dic["@downloaded"]),
                        Event = dic["@event"].ToString(),
                        PeerId = peer_id,
                        Port = Convert.ToInt32(dic["@port"])
                    });
                    _db.SaveChanges();
                }
                else
                {
                    //存在更新Tracker信息
                    entity.Ip = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                    entity.Uploaded = Convert.ToInt32(dic["@uploaded"]);
                    entity.Downloaded = Convert.ToInt32(dic["@downloaded"]);
                    entity.Left = Convert.ToInt32(dic["@left"]);
                    entity.Port = Convert.ToInt32(dic["@port"]);
                    entity.Event = dic.ContainsKey("@event") ? dic["@event"].ToString() : null;
                    _db.SaveChanges();
                }
                dic.Clear();
                //构造tracker信息列表 返回给客户端 interval 客户端心跳请求间隔 单位：秒 会间隔后自动心跳上报客户端的信息
                dic.Add("interval", 60);
                List<object> peers = new List<object>();
                _db.Bittorrents.Where(p => p.InfoHash == infoHash).ToList().ForEach(o =>
                {
                    SortedDictionary<string, object> peer = new SortedDictionary<string, object>(StringComparer.Ordinal);

                    peer.Add("peer id", o.PeerId);
                    peer.Add("ip", o.Ip);
                    peer.Add("port", o.Port);

                    peers.Add(peer);
                });
                dic.Add("peers", peers);
                return encode(dic);
            }
            catch (Exception)
            {
                throw new Exception("请遵循Tracker协议，禁止浏览器直接访问");
            }
            
        }

        public SortedDictionary<string, object> GetDic(string query)
        {
            string s = query.Substring(1);

            SortedDictionary<string, object> parameters = new SortedDictionary<string, object>(StringComparer.Ordinal);

            int num = (s != null) ? s.Length : 0;
            for (int i = 0; i < num; i++)
            {
                int startIndex = i;
                int num4 = -1;
                while (i < num)
                {
                    char ch = s[i];
                    if (ch == '=')
                    {
                        if (num4 < 0)
                        {
                            num4 = i;
                        }
                    }
                    else if (ch == '&')
                    {
                        break;
                    }
                    i++;
                }
                string str = null;
                string str2 = null;
                if (num4 >= 0)
                {
                    str = s.Substring(startIndex, num4 - startIndex);
                    str2 = s.Substring(num4 + 1, (i - num4) - 1);
                }
                else
                {
                    str2 = s.Substring(startIndex, i - startIndex);
                }

                parameters.Add("@" + str, str2);
            }
            return parameters;
        }
        public string encode(string _string)
        {
            StringBuilder string_builder = new StringBuilder();

            string_builder.Append(_string.Length);
            string_builder.Append(":");
            string_builder.Append(_string);

            return string_builder.ToString();
        }
        public string encode(int _int)
        {
            StringBuilder string_builder = new StringBuilder();

            string_builder.Append("i");
            string_builder.Append(_int);
            string_builder.Append("e");

            return string_builder.ToString();
        }
        public string encode(List<object> list)
        {
            StringBuilder string_builder = new StringBuilder();

            string_builder.Append("l");

            foreach (object _object in list)
            {
                if (_object.GetType() == typeof(string))
                {
                    string_builder.Append(encode((string)_object));
                }

                if (_object.GetType() == typeof(int))
                {
                    string_builder.Append(encode((int)_object));
                }

                if (_object.GetType() == typeof(List<object>))
                {
                    string_builder.Append(encode((List<object>)_object));
                }

                if (_object.GetType() == typeof(SortedDictionary<string, object>))
                {
                    string_builder.Append(encode((SortedDictionary<string, object>)_object));
                }
            }

            string_builder.Append("e");

            return string_builder.ToString();
        }

        public string encode(SortedDictionary<string, object> sorted_dictionary)
        {
            StringBuilder string_builder = new StringBuilder();

            string_builder.Append("d");

            foreach (KeyValuePair<string, object> key_value_pair in sorted_dictionary)
            {
                string_builder.Append(encode((string)key_value_pair.Key));

                if (key_value_pair.Value.GetType() == typeof(string))
                {
                    string_builder.Append(encode((string)key_value_pair.Value));
                }

                if (key_value_pair.Value.GetType() == typeof(int))
                {
                    string_builder.Append(encode((int)key_value_pair.Value));
                }

                if (key_value_pair.Value.GetType() == typeof(List<object>))
                {
                    string_builder.Append(encode((List<object>)key_value_pair.Value));
                }

                if (key_value_pair.Value.GetType() == typeof(SortedDictionary<string, object>))
                {
                    string_builder.Append(encode((SortedDictionary<string, object>)key_value_pair.Value));
                }
            }

            string_builder.Append("e");

            return string_builder.ToString();
        }
    }
}