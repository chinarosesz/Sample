using Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Core
{
    public class StorageTableClient
    {
        private readonly CloudStorageAccount cloudStorageAccount;
        private readonly ILogger logger;

        public CloudTableClient Client { get; private set; }

        public StorageTableClient(CloudStorageAccount cloudStorageAccount, ILogger logger)
        {
            this.logger = logger;
            this.cloudStorageAccount = cloudStorageAccount;
            ServicePoint tableServicePoint = ServicePointManager.FindServicePoint(this.cloudStorageAccount.TableEndpoint);
            tableServicePoint.UseNagleAlgorithm = false;
            tableServicePoint.Expect100Continue = false;
            tableServicePoint.ConnectionLimit = 50;
            this.Client = this.cloudStorageAccount.CreateCloudTableClient();
        }

        /// <summary>
        /// When synchronized is set to true, partitionKey must also be set
        /// </summary>
        public async Task InsertOrReplace<T>(string tableName, IEnumerable<T> entities, string partitionKey = null, bool synchronize = false) where T : CustomTableEntity, new()
        {
            // Create table if not exists
            CloudTable cloudTable = this.Client.GetTableReference(tableName);
            await cloudTable.CreateIfNotExistsAsync();

            // Add EtlIngestDate for this upsert operation
            DateTime etlIngestDate = DateTime.UtcNow;
            List<T> entitiesToUpsert = entities.ToList();
            foreach (T entity in entitiesToUpsert)
            {
                entity.EtlIngestDate = etlIngestDate;
            }

            // Chunk items to 100 per batch
            List<List<T>> batchesToInsert = new List<List<T>>();
            for (int i = 0; i < entitiesToUpsert.Count; i += 100)
            {
                batchesToInsert.Add(entitiesToUpsert.GetRange(i, Math.Min(entitiesToUpsert.Count - i, 100)));
            }

            // Upsert in parallel where each batch of upsert is 100 items
            List<Task<IList<TableResult>>> batchTasks = new List<Task<IList<TableResult>>>();
            foreach (List<T> batchToInsert in batchesToInsert)
            {
                TableBatchOperation batchOperation = new TableBatchOperation();
                foreach (TableEntity tableEntity in batchToInsert)
                {
                    batchOperation.InsertOrReplace(tableEntity);
                }

                Task<IList<TableResult>> task = cloudTable.ExecuteBatchAsync(batchOperation);
                batchTasks.Add(task);
            }
            this.logger.LogInformation($"Upserting batch with {entitiesToUpsert.Count} items");
            await Task.WhenAll(batchTasks);
            this.logger.LogInformation("Upsert succeeded");

            // Removes deleted records that are not part of the entity for this query and partition key
            if (synchronize == true && partitionKey != null)
            {
                string query = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterConditionForDate("EtlIngestDate", QueryComparisons.NotEqual, etlIngestDate),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

                List<TableEntity> foundEntities = await this.GetTableEntitiesAsync<TableEntity>(cloudTable, query);
                this.BulkDelete(cloudTable, foundEntities);
            }
        }

        public async Task<List<T>> GetTableEntitiesAsync<T>(CloudTable cloudTable, string query) where T : ITableEntity, new()
        {
            TableQuery<T> tableQuery = new TableQuery<T>().Where(query);

            TableContinuationToken continuationToken = null;
            List<T> retrievedEntities = new List<T>();
            do
            {
                TableQuerySegment<T> tableQueryResult = await cloudTable.ExecuteQuerySegmentedAsync(tableQuery, continuationToken);

                continuationToken = tableQueryResult.ContinuationToken;
                foreach (T result in tableQueryResult.Results)
                {
                    retrievedEntities.Add(result);
                }
            }
            while (continuationToken != null);

            return retrievedEntities;
        }

        public void BulkDelete(CloudTable cloudTable, IEnumerable<TableEntity> entities)
        {
            // Chunk items to 100 per batch
            List<TableEntity> entitites = entities.ToList();
            List<List<TableEntity>> batchesToInsert = new List<List<TableEntity>>();
            for (int i = 0; i < entitites.Count; i += 100)
            {
                batchesToInsert.Add(entitites.GetRange(i, Math.Min(entitites.Count - i, 100)));
            }

            // Bulk delete
            this.logger.LogInformation($"Deleting {batchesToInsert.Count} batches");
            Parallel.ForEach(batchesToInsert, async (batchToInsert) =>
            {
                TableBatchOperation batchOperation = new TableBatchOperation();
                foreach (TableEntity entity in batchToInsert)
                {
                    batchOperation.Delete(entity);
                }

                this.logger.LogInformation($"Deleting batch with {batchToInsert.Count} items");
                await cloudTable.ExecuteBatchAsync(batchOperation);
            });
        }
    }
}
