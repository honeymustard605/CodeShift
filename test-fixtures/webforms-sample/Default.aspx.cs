using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;

public partial class Default : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            BindGrid();
        }
    }

    private void BindGrid()
    {
        var connStr = System.Configuration.ConfigurationManager
            .ConnectionStrings["DefaultConnection"].ConnectionString;

        using var conn = new SqlConnection(connStr);
        using var cmd = new SqlCommand("SELECT * FROM Orders WHERE Active = 1", conn);
        conn.Open();
        var reader = cmd.ExecuteReader();
        GridView1.DataSource = reader;
        GridView1.DataBind();
    }

    protected void Button1_Click(object sender, EventArgs e)
    {
        var user = HttpContext.Current.User.Identity.Name;
        Session["LastAction"] = $"Submitted by {user} at {DateTime.Now}";
        Response.Redirect("~/Confirmation.aspx");
    }
}
