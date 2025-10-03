namespace SOP.Repositories
{
    public enum ArchiveStatus { Archived, NotFound, InUse }
    public sealed record ArchiveResult<T>(ArchiveStatus Status, T? Entity)
    {
        public static ArchiveResult<T> NotFound() => new(ArchiveStatus.NotFound, default);
        public static ArchiveResult<T> InUse(T? entity) => new(ArchiveStatus.InUse, entity);
        public static ArchiveResult<T> Archived(T entity) => new(ArchiveStatus.Archived, entity);
    }

    public enum DeleteStatus { Deleted, NotFound, InUse }
    public sealed record DeleteResult<T>(DeleteStatus Status, T? Entity)
    {
        public static DeleteResult<T> NotFound() => new(DeleteStatus.NotFound, default);
        public static DeleteResult<T> InUse(T? entity) => new(DeleteStatus.InUse, entity);
        public static DeleteResult<T> Deleted(T entity) => new(DeleteStatus.Deleted, entity);
    }
}
