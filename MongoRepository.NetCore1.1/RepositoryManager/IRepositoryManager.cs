using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoRepository
{
    /// <summary>
    ///     IRepositoryManager definition.
    /// </summary>
    /// <typeparam name="T">The type contained in the repository to manage.</typeparam>
    /// <typeparam name="TKey">The type used for the entity's Id.</typeparam>
    public interface IRepositoryManager<T, TKey>
        where T : IEntity<TKey>
    {
        /// <summary>
        ///     Gets the name of the collection as Mongo uses.
        /// </summary>
        /// <value>The name of the collection as Mongo uses.</value>
        string Name { get; }

        /// <summary>
        ///     Drops the repository.
        /// </summary>
        void Drop();


        /// <summary>
        ///     Drops all indexes on this repository.
        /// </summary>
        void DropAllIndexes();


        /// <summary>
        ///     Gets the indexes for this repository.
        /// </summary>
        /// <returns>Returns the indexes for this repository.</returns>
        IAsyncCursor<BsonDocument> GetIndexes();
    }

    /// <summary>
    ///     IRepositoryManager definition.
    /// </summary>
    /// <typeparam name="T">The type contained in the repository to manage.</typeparam>
    /// <remarks>Entities are assumed to use strings for Id's.</remarks>
    public interface IRepositoryManager<T> : IRepositoryManager<T, string>
        where T : IEntity<string>
    {
    }
}