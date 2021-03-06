﻿using System;
using System.Collections.Generic;

namespace OpenDMS.Storage.Providers.CouchDB.Transactions.Tasks
{
    public class UploadBulkDocuments : Base
    {
        private IDatabase _db;
        private Model.BulkDocuments _bulkDocuments;

        public List<Commands.PostBulkDocumentsReply.Entry> Results { get; private set; }

        public UploadBulkDocuments(IDatabase db, Model.BulkDocuments bulkDocs,
            int sendTimeout, int receiveTimeout, int sendBufferSize, int receiveBufferSize)
            : base(sendTimeout, receiveTimeout, sendBufferSize, receiveBufferSize)
        {
            _db = db;
            _bulkDocuments = bulkDocs;
        }

        public Commands.PostBulkDocumentsReply.Entry FindEntryById(string id)
        {
            if (Results == null)
                return null;

            for (int i = 0; i < Results.Count; i++)
            {
                if (Results[i].Id == id)
                    return Results[i];
            }

            return null;
        }

        public override void Process()
        {
            Remoting.SaveBulk rem;

            TriggerOnActionChanged(EngineActionType.UploadingBulk, true);

            try
            {
                rem = new Remoting.SaveBulk(_db, _bulkDocuments, _sendTimeout, _receiveTimeout,
                    _sendBufferSize, _receiveBufferSize);
            }
            catch (Exception e)
            {
                Logger.Storage.Error("An exception occurred while instantiating the Transactions.Tasks.Remoting.SaveBulk object.", e);
                throw;
            }

            rem.OnComplete += delegate(Remoting.Base sender, ICommandReply reply)
            {
                Results = ((Commands.PostBulkDocumentsReply)reply).Results;
                TriggerOnComplete(reply);
            };
            rem.OnError += delegate(Remoting.Base sender, string message, Exception exception)
            {
                TriggerOnError(message, exception);
            };
            rem.OnProgress += delegate(Remoting.Base sender, OpenDMS.Networking.Protocols.Tcp.DirectionType direction, int packetSize, decimal sendPercentComplete, decimal receivePercentComplete)
            {
                TriggerOnProgress(direction, packetSize, sendPercentComplete, receivePercentComplete);
            };
            rem.OnTimeout += delegate(Remoting.Base sender)
            {
                TriggerOnTimeout();
            };

            rem.Process();
        }
    }
}
