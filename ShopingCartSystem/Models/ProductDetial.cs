namespace ShopingCartSystem.Models
{
	public class ProductDetial
	{
		public class ProductDTO
		{
			public List<DataProductDTO> Products { get; set; }
			public string SumTotal { get; set; }
		}
		public class DataProductDTO
		{
			public int id { get; set; }
			public string ProductName { get; set; }
			public string ProductPrice { get; set; }
			public int ProductUnit { get; set; }
			public string Total { get; set; }
			public string SumTotal { get; set; }
		}

		public class getUpdateID
		{
			public int id { get; set; }
		}
		
	}
}
