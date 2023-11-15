namespace api.Data.Repositories;
public interface IBaseRepository<T>
{
    T? GetById(int id);
    IEnumerable<T> GetAll();
    void Add(T entity);
    Task UpdateAsync(T entity);
    void Delete(int id);
}
