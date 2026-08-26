using AttendVisionReportsApi.DTOs;
using Dapper;
using Npgsql;

namespace AttendVisionReportsApi.Services
{
	public class CompanyService(NpgsqlDataSource dataSource) : ICompanyService
	{
		// display_name is the only snake_case (underscored) column on this
		// table, so it's the only one that needs an explicit AS alias -
		// unquoted "DisplayName" would otherwise fold to "displayname" and
		// Postgres would report it as a nonexistent column.
		private const string SelectColumns = "Id, Name, display_name AS \"DisplayName\", Description, Phone, Email, Address";

		public async Task<IEnumerable<CompanyDto>> GetAllAsync()
		{
			using var conn = dataSource.CreateConnection();
			var sql = $"SELECT {SelectColumns} FROM companies ORDER BY Name";
			return await conn.QueryAsync<CompanyDto>(sql);
		}

		public async Task<CompanyDto?> GetByIdAsync(Guid id)
		{
			using var conn = dataSource.CreateConnection();
			var sql = $"SELECT {SelectColumns} FROM companies WHERE Id = @id";
			return await conn.QueryFirstOrDefaultAsync<CompanyDto>(sql, new { id });
		}

		public async Task<CompanyDto> CreateAsync(CreateCompanyDto dto)
		{
			using var conn = dataSource.CreateConnection();
			var sql = $"INSERT INTO companies (Name, display_name, Description, Phone, Email, Address) VALUES (@Name, @DisplayName, @Description, @Phone, @Email, @Address) RETURNING {SelectColumns}";
			return await conn.QuerySingleAsync<CompanyDto>(sql, dto);
		}

		public async Task<bool> UpdateAsync(Guid id, UpdateCompanyDto dto)
		{
			using var conn = dataSource.CreateConnection();
			var sql = "UPDATE companies SET Name = @Name, display_name = @DisplayName, Description = @Description, Phone = @Phone, Email = @Email, Address = @Address WHERE Id = @Id";
			var affected = await conn.ExecuteAsync(sql, new { dto.Name, dto.DisplayName, dto.Description, dto.Phone, dto.Email, dto.Address, Id = id });
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