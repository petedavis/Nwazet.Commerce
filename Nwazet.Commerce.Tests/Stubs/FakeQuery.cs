﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Records;

namespace Nwazet.Commerce.Tests.Stubs {
    public class FakeQuery<TPart> : IContentQuery<TPart> where TPart:IContent {
        private string[] _types;

        public FakeQuery(ContentManagerStub contentManager) {
            ContentManager = contentManager;
        }
 
        public IContentManager ContentManager { get; private set; }
        private ContentManagerStub ContentManagerStub {
            get { return (ContentManagerStub) ContentManager; }
        }

        public IContentQuery<TPart> ForType(params string[] contentTypes) {
            _types = contentTypes;
            return this;
        }

        public IEnumerable<TPart> List() {
            var itemsOfThatType = ContentManagerStub
                .GetAllItems()
                .Where(i => i.Has<TPart>())
                .Select(i => i.As<TPart>());
            if (_types == null || !_types.Any()) {
                return itemsOfThatType;
            }
            return itemsOfThatType
                .Where(i => _types.Contains(i.ContentItem.ContentType));
        }

        public IContentQuery<TPart1> ForPart<TPart1>() where TPart1 : IContent {
            return new FakeQuery<TPart1>(ContentManagerStub);
        }

        public IContentQuery<TPart> ForVersion(VersionOptions options) {
            throw new NotImplementedException();
        }

        public IEnumerable<TPart> Slice(int skip, int count) {
            throw new NotImplementedException();
        }

        public int Count() {
            throw new NotImplementedException();
        }

        public IContentQuery<TPart, TRecord> Join<TRecord>() where TRecord : ContentPartRecord {
            throw new NotImplementedException();
        }

        public IContentQuery<TPart, TRecord> Where<TRecord>(Expression<Func<TRecord, bool>> predicate) where TRecord : ContentPartRecord {
            throw new NotImplementedException();
        }

        public IContentQuery<TPart, TRecord> OrderBy<TRecord>(Expression<Func<TRecord, object>> keySelector) where TRecord : ContentPartRecord {
            throw new NotImplementedException();
        }

        public IContentQuery<TPart, TRecord> OrderByDescending<TRecord>(Expression<Func<TRecord, object>> keySelector) where TRecord : ContentPartRecord {
            throw new NotImplementedException();
        }
    }
}