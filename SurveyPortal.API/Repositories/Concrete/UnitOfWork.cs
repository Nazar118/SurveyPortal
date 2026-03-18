using SurveyPortal.API.Data;
using SurveyPortal.API.Repositories.Interfaces;

namespace SurveyPortal.API.Repositories.Concrete
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        public UnitOfWork(AppDbContext context) => _context = context;

        public void Commit() => _context.SaveChanges();
        public async Task CommitAsync() => await _context.SaveChangesAsync();
    }
}