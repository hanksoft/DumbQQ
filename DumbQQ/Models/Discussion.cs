﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using DumbQQ.Constants;
using DumbQQ.Models.Abstract;
using DumbQQ.Models.Receipts;
using DumbQQ.Utils;
using RestSharp;
using RestSharp.Deserializers;
using SimpleJson;

namespace DumbQQ.Models
{
    public class Discussion : UserCollection<Discussion.Member>, IUseLazyProperty, IMessageTarget
    {
        #region properties

        public class Member : User
        {
            [DeserializeAs(Name = @"uin")]
            public override ulong Id { get; internal set; }

            [DeserializeAs(Name = @"nick")]
            public override string Name { get; internal set; }

            public override string NameAlias => null;
        }

        protected enum LazyProperty
        {
            Members
        }

        internal Discussion()
        {
            Properties = new LazyProperties(() =>
            {
                var response =
                    Client.RestClient.Get<DiscussionPropertiesReceipt>(Api.GetDiscussInfo.Get(Id,
                        Client.Session.tokens.Vfwebqq,
                        Client.Session.tokens.Psessionid));
                if (!response.IsSuccessful)
                    throw new HttpRequestException($"HTTP request unsuccessful: status code {response.StatusCode}");

                return new Dictionary<int, object>
                {
                    {
                        (int) LazyProperty.Members,
                        new ReadOnlyDictionary<ulong, Member>(response.Data.Result.MemberList.Reassemble(x => x.Id, Client,
                            response.Data.Result.MemberStatusList))
                    }
                };
            });
        }

        protected readonly LazyProperties Properties;

        [DeserializeAs(Name = @"did")]
        public override ulong Id { get; internal set; }

        [DeserializeAs(Name = @"name")]
        public override string Name { get; internal set; }

        public override ReadOnlyDictionary<long, Member> Members => Properties[(int) LazyProperty.Members];
        public override IEnumerator<Member> GetEnumerator() => Members.Values.GetEnumerator();
        public void LoadLazyProperties() => Properties.Load();

        #endregion

        public void Message(string content)
        {
            var response = Client.RestClient.Post<MessageReceipt>(Api.SendMessageToDiscuss.Post(
                new JsonObject
                {
                    {@"did", Id},
                    {@"content", new JsonArray {content, new JsonArray {@"font", Miscellaneous.Font}}.ToString()},
                    {@"face", 573},
                    {@"client_id", Miscellaneous.ClientId},
                    {@"msg_id", Miscellaneous.MessageId},
                    {@"psessionid", Client.Session.tokens.Psessionid}
                }));
            if (!response.IsSuccessful)
                throw new HttpRequestException($"HTTP request unsuccessful: status code {response.StatusCode}");
            if (response.Data.Code != 0)
                throw new ApplicationException($"Request unsuccessful: returned {response.Data.Code}");
        }
    }
}