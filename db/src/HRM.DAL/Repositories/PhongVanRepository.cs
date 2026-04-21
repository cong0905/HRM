using HRM.DAL.Context;
using HRM.Domain.Entities;

namespace HRM.DAL.Repositories
{
    public class PhongVanRepository : Repository<PhongVan>, IPhongVanRepository
    {
        public PhongVanRepository(HrmDbContext context) : base(context)
        {
        }
    }
}
