using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoRepository
{
    /// <summary>
    ///     Deals with entities in MongoDb.
    /// </summary>
    /// <typeparam name="T">The type contained in the repository.</typeparam>
    /// <typeparam name="TKey">The type used for the entity's Id.</typeparam>
    public class MongoRepository<T, TKey> : IRepository<T, TKey>
        where T : IEntity<TKey>
    {
        /// <summary>
        ///     MongoCollection field.
        /// </summary>
        protected internal IMongoCollection<T> collection;

        /// <summary>
        ///     Initializes a new instance of the MongoRepository class.
        ///     Uses the Default App/Web.Config connectionstrings to fetch the connectionString and Database name.
        /// </summary>
        /// <param name="opt"></param>
        /// <remarks>Default constructor defaults to "MongoServerSettings" key for connectionstring.</remarks>
        public MongoRepository(IOptions<DatabaseSettings> opt)
            : this(Util<TKey>.GetDefaultConnectionString(opt))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        public MongoRepository(string connectionString)
        {
            collection = Util<TKey>.GetCollectionFromConnectionString<T>(connectionString);
        }

        /// <summary>
        ///     Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        public MongoRepository(string connectionString, string collectionName)
        {
            collection = Util<TKey>.GetCollectionFromConnectionString<T>(connectionString, collectionName);
        }

        /// <summary>
        ///     Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="url">Url to use for connecting to MongoDB.</param>
        public MongoRepository(MongoUrl url)
        {
            collection = Util<TKey>.GetCollectionFromUrl<T>(url);
        }

        /// <summary>
        ///     Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="url">Url to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        public MongoRepository(MongoUrl url, string collectionName)
        {
            collection = Util<TKey>.GetCollectionFromUrl<T>(url, collectionName);
        }

        /// <summary>
        ///     Gets the name of the collection
        /// </summary>
        public string CollectionName => collection.CollectionNamespace.CollectionName;

        /// <summary>
        ///     Gets the Mongo collection (to perform advanced operations).
        /// </summary>
        /// <remarks>
        ///     One can argue that exposing this property (and with that, access to it's Database property for instance
        ///     (which is a "parent")) is not the responsibility of this class. Use of this property is highly discouraged;
        ///     for most purposes you can use the MongoRepositoryManager&lt;T&gt;
        /// </remarks>
        /// <value>The Mongo collection (to perform advanced operations).</value>
        public IMongoCollection<T> Collection => collection;

        /// <summary>
        ///     Returns the T by its given id.
        /// </summary>
        /// <param name="id">The Id of the entity to retrieve.</param>
        /// <returns>The Entity T.</returns>
        public virtual T GetById(TKey id)
        {
            if (typeof(T).GetTypeInfo().IsSubclassOf(typeof(Entity)))
                return GetById(new ObjectId(id as string));

            return collection.FindAsync(Builders<T>.Filter.Eq("_id", BsonValue.Create(id))).Result.SingleOrDefault();
        }

        /// <summary>
        ///     Adds the new entity in the repository.
        /// </summary>
        /// <param name="entity">The entity T.</param>
        /// <returns>The added entity including its new ObjectId.</returns>
        public virtual T Add(T entity)
        {
            collection.InsertOne(entity);

            return entity;
        }

        /// <summary>
        ///     Adds the new entities in the repository.
        /// </summary>
        /// <param name="entities">The entities of type T.</param>
        public virtual void Add(IEnumerable<T> entities)
        {
            collection.InsertMany(entities);
        }

        /// <summary>
        ///     Upserts an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The updated entity.</returns>
        public virtual T Update(T entity)
        {
            collection.FindOneAndReplace(Builders<T>.Filter.Eq("_id", BsonValue.Create(entity.Id)), entity);

            return entity;
        }

        /// <summary>
        ///     Upserts the entities.
        /// </summary>
        /// <param name="entities">The entities to update.</param>
        public virtual void Update(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
                collection.FindOneAndReplace(Builders<T>.Filter.Eq("_id", BsonValue.Create(entity.Id)), entity);
        }

        /// <summary>
        ///     Deletes an entity from the repository by its id.
        /// </summary>
        /// <param name="id">The entity's id.</param>
        public virtual void Delete(TKey id)
        {
            collection.DeleteOne(typeof(T).GetTypeInfo().IsSubclassOf(typeof(Entity))
                ? Builders<T>.Filter.Eq("_id", new ObjectId(id as string))
                : Builders<T>.Filter.Eq("_id", BsonValue.Create(id)));
        }

        /// <summary>
        ///     Deletes the given entity.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        public virtual void Delete(T entity)
        {
            Delete(entity.Id);
        }

        /// <summary>
        ///     Deletes the entities matching the predicate.
        /// </summary>
        /// <param name="predicate">The expression.</param>
        public virtual void Delete(Expression<Func<T, bool>> predicate)
        {
            foreach (var entity in collection.AsQueryable().Where(predicate))
                Delete(entity.Id);
        }

        /// <summary>
        ///     Deletes all entities in the repository.
        /// </summary>
        public virtual void DeleteAll()
        {
            collection.DeleteMany(Builders<T>.Filter.Empty);
        }

        /// <summary>
        ///     Counts the total entities in the repository.
        /// </summary>
        /// <returns>Count of entities in the collection.</returns>
        public virtual long Count()
        {
            return collection.Count(Builders<T>.Filter.Empty);
        }

        /// <summary>
        ///     Checks if the entity exists for given predicate.
        /// </summary>
        /// <param name="predicate">The expression.</param>
        /// <returns>True when an entity matching the predicate exists, false otherwise.</returns>
        public virtual bool Exists(Expression<Func<T, bool>> predicate)
        {
            return collection.AsQueryable().Any(predicate);
        }

        /// <summary>
        ///     Returns the T by its given id.
        /// </summary>
        /// <param name="id">The Id of the entity to retrieve.</param>
        /// <returns>The Entity T.</returns>
        public virtual T GetById(ObjectId id)
        {
            var queryId = Builders<T>.Filter.Eq("_id", id);
            var entity = collection.FindAsync<T>(queryId);
            return entity.Result.FirstOrDefault();
        }

        /// <summary>
        ///     Deletes an entity from the repository by its ObjectId.
        /// </summary>
        /// <param name="id">The ObjectId of the entity.</param>
        public virtual void Delete(ObjectId id)
        {
            collection.DeleteOne(Builders<T>.Filter.Eq("_id", id));
        }

        #region IQueryable<T>

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An IEnumerator&lt;T&gt; object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<T> GetEnumerator()
        {
            return collection.AsQueryable().GetEnumerator();
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An IEnumerator object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return collection.AsQueryable().GetEnumerator();
        }

        /// <summary>
        ///     Gets the type of the element(s) that are returned when the expression tree associated with this instance of
        ///     IQueryable is executed.
        /// </summary>
        public virtual Type ElementType => collection.AsQueryable().ElementType;

        /// <summary>
        ///     Gets the expression tree that is associated with the instance of IQueryable.
        /// </summary>
        public virtual Expression Expression => collection.AsQueryable().Expression;

        /// <summary>
        ///     Gets the query provider that is associated with this data source.
        /// </summary>
        public virtual IQueryProvider Provider => collection.AsQueryable().Provider;

        IMongoCollection<T> IRepository<T, TKey>.Collection
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }

    /// <summary>
    ///     Deals with entities in MongoDb.
    /// </summary>
    /// <typeparam name="T">The type contained in the repository.</typeparam>
    /// <remarks>Entities are assumed to use strings for Id's.</remarks>
    public class MongoRepository<T> : MongoRepository<T, string>
        where T : IEntity<string>
    {
        /// <summary>
        ///     Initializes a new instance of the MongoRepository class.
        ///     Uses the Default App/Web.Config connectionstrings to fetch the connectionString and Database name.
        /// </summary>
        /// <param name="opt"></param>
        /// <remarks>Default constructor defaults to "MongoServerSettings" key for connectionstring.</remarks>
        public MongoRepository(IOptions<DatabaseSettings> opt) : base(opt)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="url">Url to use for connecting to MongoDB.</param>
        public MongoRepository(MongoUrl url)
            : base(url)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="url">Url to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        public MongoRepository(MongoUrl url, string collectionName)
            : base(url, collectionName)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        public MongoRepository(string connectionString)
            : base(connectionString)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        public MongoRepository(string connectionString, string collectionName)
            : base(connectionString, collectionName)
        {
        }
    }
}