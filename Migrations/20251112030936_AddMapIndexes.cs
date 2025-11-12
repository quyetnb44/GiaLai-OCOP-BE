using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiaLaiOCOP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMapIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🔹 Indexes cho performance - Map queries
            
            // Index cho tọa độ (quan trọng nhất cho bounding box và nearby queries)
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Enterprises_Latitude_Longitude"" 
                ON ""Enterprises"" (""Latitude"", ""Longitude"") 
                WHERE ""Latitude"" IS NOT NULL AND ""Longitude"" IS NOT NULL;
            ");

            // Index cho District và Province (cho filter)
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Enterprises_District"" 
                ON ""Enterprises"" (""District"") 
                WHERE ""District"" != '';
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Enterprises_Province"" 
                ON ""Enterprises"" (""Province"") 
                WHERE ""Province"" != '';
            ");

            // Index cho OCOPRating (cho filter)
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Enterprises_OCOPRating"" 
                ON ""Enterprises"" (""OCOPRating"") 
                WHERE ""OCOPRating"" IS NOT NULL;
            ");

            // Index cho BusinessField (cho filter)
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Enterprises_BusinessField"" 
                ON ""Enterprises"" (""BusinessField"") 
                WHERE ""BusinessField"" != '';
            ");

            // Index cho Name (cho search)
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Enterprises_Name"" 
                ON ""Enterprises"" (LOWER(""Name""));
            ");

            // Index cho Products - OCOPRating và StockStatus
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Products_OCOPRating"" 
                ON ""Products"" (""OCOPRating"") 
                WHERE ""OCOPRating"" IS NOT NULL;
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Products_StockStatus"" 
                ON ""Products"" (""StockStatus"");
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Enterprises_Latitude_Longitude"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Enterprises_District"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Enterprises_Province"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Enterprises_OCOPRating"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Enterprises_BusinessField"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Enterprises_Name"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Products_OCOPRating"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Products_StockStatus"";");

        }
    }
}
