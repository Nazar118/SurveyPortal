using System.Linq.Expressions;

namespace SurveyPortal.API.Repositories.Interfaces
{
    // T bir sınıf (Entity) olacak şekilde genel bir arayüz tanımlıyoruz
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id); 
        Task<IEnumerable<T>> GetAllAsync(); // Hepsini getir
        IQueryable<T> Where(Expression<Func<T, bool>> expression); //Filtrele
        Task AddAsync(T entity); 
        void Update(T entity); 
        void Remove(T entity); 
    }
}