using AttendVisionReportsApi.DTOs;
using Dapper;
using Npgsql;

namespace AttendVisionReportsApi.Services
{
	public class CompanyService(NpgsqlDataSource dataSource) : ICompanyService
	{
		public async Task<IEnumerable<CompanyDto>> GetAllAsync()
		{
			using var conn = dataSource.CreateConnection();
			var sql = "SELECT Id, Name, Description FROM companies ORDER BY Name";
			return await conn.QueryAsync<CompanyDto>(sql);
		}

		public async Task<CompanyDto?> GetByIdAsync(Guid id)
		{
			using var conn = dataSource.CreateConnection();
			var sql = "SELECT Id, Name, Description FROM companies WHERE Id = @id";
			return await conn.QueryFirstOrDefaultAsync<CompanyDto>(sql, new { id });
		}

		public async Task<CompanyDto> CreateAsync(CreateCompanyDto dto)
		{
			using var conn = dataSource.CreateConnection();
			var sql = "INSERT INTO companies (Name, Description) VALUES (@Name, @Description) RETURNING Id, Name, Description";
			return await conn.QuerySingleAsync<CompanyDto>(sql, dto);
		}

		public async Task<bool> UpdateAsync(Guid id, UpdateCompanyDto dto)
		{
			using var conn = dataSource.CreateConnection();
			var sql = "UPDATE companies SET Name = @Name, Description = @Description WHERE Id = @Id";
			var affected = await conn.ExecuteAsync(sql, new { dto.Name, dto.Description, Id = id });
			return affected > 0;
		}

		public async Task<bool> DeleteAsync(Guid id)
		{
			using var conn = dataSource.CreateConnection();
			var sql = "DELETE FROM companies WHERE Id = @id";
			var affected = await conn.ExecuteAsync(sql, new { id });
			return affected > 0;
		}
	}
}