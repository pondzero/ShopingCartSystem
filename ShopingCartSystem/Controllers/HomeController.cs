using Microsoft.AspNetCore.Mvc;
using ShopingCartSystem.Models;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using static ShopingCartSystem.Models.ProductDetial;

namespace ShopingCartSystem.Controllers
{
    public class HomeController : Controller
    {
		private readonly IConfiguration _config;
		private string DefaultConnection;

		public HomeController(IConfiguration config)
		{
			_config = config;
			DefaultConnection = _config.GetConnectionString("DefaultConnection");
		}

		public IActionResult Index()
        {
			var viewModel = new ProductDTO();
			List<DataProductDTO> ProductDetials = new List<DataProductDTO>();

			using (SqlConnection connection = new SqlConnection(DefaultConnection))
			{
				connection.Open();

				using (SqlCommand command = new SqlCommand($@"SELECT A.ID, A.ProductName, B.Unit, A.Price ,COALESCE(D.Unit,0) as CartUnit 
															FROM ProductDATA AS A 
															INNER JOIN StockDATA AS B ON A.ID = B.ProductID 
															left join TempCart as D on A.ID = D.ProductID", connection))
				{
					SqlDataAdapter adapter = new SqlDataAdapter(command);
					DataSet dataSet = new DataSet();
					var table = new DataTable();
					adapter.Fill(dataSet); // Ensure connection is open before filling
					adapter.Fill(table);

					foreach (DataRow row in table.Rows)
					{
						DataProductDTO ProductDetial = new DataProductDTO();
						ProductDetial.id = (int)row[0];
						ProductDetial.ProductName = row["ProductName"].ToString();
						ProductDetial.ProductUnit = (int)row["Unit"] - (int)row["CartUnit"];
						ProductDetial.ProductPrice = row["Price"].ToString();
						ProductDetials.Add(ProductDetial);
					}
					viewModel.Products = ProductDetials;

					connection.Close(); // Close the connection when finished
				}
			}

			return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Cart()
        {
			var viewModel = new ProductDTO();
			List<DataProductDTO> ProductDetials = new List<DataProductDTO>();
			string getSumtotal = string.Empty;
			using (SqlConnection connection = new SqlConnection(DefaultConnection))
			{
				connection.Open();

				using (SqlCommand command = new SqlCommand($@"select A.ProductID,B.ProductName,A.Unit,B.Price,B.Price*A.Unit as Total,
															(select SUM(B.Price*A.Unit) as CountTotal
															from TempCart a inner join ProductDATA B on a.ProductID =  B.ID 
															where A.Unit<>0) as SumTotal
															from TempCart a inner join ProductDATA B on a.ProductID =  B.ID where A.Unit<>0", connection))
				{
					SqlDataAdapter adapter = new SqlDataAdapter(command);
					DataSet dataSet = new DataSet();
					var table = new DataTable();
					adapter.Fill(dataSet); // Ensure connection is open before filling
					adapter.Fill(table);

					foreach (DataRow row in table.Rows)
					{
						DataProductDTO ProductDetial = new DataProductDTO();
						ProductDetial.id = (int)row[0];
						ProductDetial.ProductName = row["ProductName"].ToString();
						ProductDetial.ProductUnit = (int)row["Unit"];
						ProductDetial.ProductPrice = row["Price"].ToString();
						ProductDetial.Total = Convert.ToInt32( row["Total"].ToString()).ToString("N2");
						ProductDetial.SumTotal = Convert.ToUInt32(row["SumTotal"]).ToString("N2");
						getSumtotal = Convert.ToUInt32(row["SumTotal"]).ToString("N2");
                        ProductDetials.Add(ProductDetial);
					}
					viewModel.Products = ProductDetials;
					viewModel.SumTotal = getSumtotal;


                    connection.Close(); // Close the connection when finished
				}
			}

			return View(viewModel);
		}

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


		[HttpPost]
		public IActionResult testPost([FromBody] string value)
		{
			// Logic to process the posted data
			var response = new { message = $"Received: {value}" };
			return Ok(response);
		}

		[HttpPost]
		public IActionResult AddCart([FromBody] getUpdateID getID)
		{
			using (SqlConnection connection = new SqlConnection(DefaultConnection))
			{
				connection.Open();

				string updateQuery = $@"UPDATE TempCart SET Unit = (Select Unit from TempCart where ProductID={getID.id})+1 WHERE ProductID = {getID.id}";

				using (SqlCommand command = new SqlCommand(updateQuery, connection))
				{
					// Execute the update command
					int rowsAffected = command.ExecuteNonQuery();
					// Process the result if needed
					if (rowsAffected > 0)
					{
						// Update successful
					}
					else
					{
						// No rows were affected, handle accordingly
					}
				}
			}
			return Ok();
		}

        [HttpPost]
        public IActionResult DelCart([FromBody] getUpdateID getID)
        {
            using (SqlConnection connection = new SqlConnection(DefaultConnection))
            {
                connection.Open();

                string updateQuery = $@"UPDATE TempCart SET Unit = 0 WHERE ProductID = {getID.id}";

                using (SqlCommand command = new SqlCommand(updateQuery, connection))
                {
                    // Execute the update command
                    int rowsAffected = command.ExecuteNonQuery();
                    // Process the result if needed
                    if (rowsAffected > 0)
                    {
                        // Update successful
                    }
                    else
                    {
                        // No rows were affected, handle accordingly
                    }
                }
            }
            return Ok();
        }

		[HttpPost]
		public IActionResult CheckOut([FromBody] getUpdateID getID)
		{
			using (SqlConnection connection = new SqlConnection(DefaultConnection))
			{
				connection.Open();

				using (SqlCommand command = new SqlCommand($@"select A.ProductID,A.Unit from TempCart a inner join ProductDATA B on a.ProductID =  B.ID where A.Unit<>0", connection))
				{
					SqlDataAdapter adapter = new SqlDataAdapter(command);
					DataSet dataSet = new DataSet();
					var table = new DataTable();
					adapter.Fill(dataSet); // Ensure connection is open before filling
					adapter.Fill(table);

					foreach (DataRow row in table.Rows)
					{
						UpdateStock((int)row["ProductID"]);
						UpdateTemp((int)row["ProductID"]);
                    }
				}
			}
            return Ok();
        }

		private void UpdateStock(int getID)
		{
            using (SqlConnection connection = new SqlConnection(DefaultConnection))
            {
                connection.Open();

                string updateQuery = $@"UPDATE StockDATA SET Unit = (Select B.Unit - A.Unit as NewValue from TempCart A inner join StockDATA B on A.ProductID = B.ProductID where A.ProductID={getID}) WHERE ProductID = {getID};";

                using (SqlCommand command = new SqlCommand(updateQuery, connection))
                {
                    // Execute the update command
                    int rowsAffected = command.ExecuteNonQuery();
                    // Process the result if needed
                    if (rowsAffected > 0)
                    {
                        // Update successful
                    }
                    else
                    {
                        // No rows were affected, handle accordingly
                    }
                }
				connection.Close();
            }
        }

        private void UpdateTemp(int getID)
        {
            using (SqlConnection connection = new SqlConnection(DefaultConnection))
            {
                connection.Open();

                string updateQuery = $@"UPDATE TempCart SET Unit = 0 WHERE ProductID = {getID}";

                using (SqlCommand command = new SqlCommand(updateQuery, connection))
                {
                    // Execute the update command
                    int rowsAffected = command.ExecuteNonQuery();
                    // Process the result if needed
                    if (rowsAffected > 0)
                    {
                        // Update successful
                    }
                    else
                    {
                        // No rows were affected, handle accordingly
                    }
                }
            }
        }
    }
}