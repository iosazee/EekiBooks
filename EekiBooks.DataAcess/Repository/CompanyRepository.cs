using EekiBooks.DataAccess;
using EekiBooks.DataAcess.Repository.IRepository;
using EekiBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EekiBooks.DataAcess.Repository
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        private ApplicationDbContext _db;

        public CompanyRepository(ApplicationDbContext db) : base(db) 
        {
            _db = db;
        }


        public void Update(Company obj)
        {
            _db.Companiess.Update(obj);
        }
    }
}
