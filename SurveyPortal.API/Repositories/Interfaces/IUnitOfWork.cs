namespace SurveyPortal.API.Repositories.Interfaces
{
    public interface IUnitOfWork
    {
        Task CommitAsync(); // Kaydet (Asenkron)
        void Commit();      // Kaydet
    }
}