using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalladiumSmsService
{
    public sealed class SmsService
    {
        public async Task sendinvoicenotification()
        {
            //WriteToFile("sending invoice notification starting");
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            IConfiguration configuration = builder.Build();
            string constring = configuration.GetConnectionString("notifications");
            string? recipient;
            try
            {

                using (SqlConnection cnn = new SqlConnection(constring))
                {
                    //_logger.LogInformation("Getting to querry data: {time}", DateTimeOffset.Now);
                    //WriteToFile("Getting to querry data");
                    //cmd = new SqlCommand("sp_insert_register", xc);
                    SqlCommand cmd = new SqlCommand("select * from qryinvoices where notificationsent=@notificationsent");
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@notificationsent", "0");
                    cmd.Parameters.AddWithValue("@DateConsumed", DateTime.Now);
                    cnn.Open();
                    cmd.Connection = cnn;
                    SqlDataReader myReader = cmd.ExecuteReader();
                    while (myReader.Read())
                    {


                        //_logger.LogInformation("reader found data: {time}", DateTimeOffset.Now);
                        // WriteToFile("Data found");
                        //_logger.
                        try
                        {
                            using (var client = new HttpClient())
                            {

                                DateTime expectedDate = DateTime.Parse(myReader["dteRequired"].ToString());

                                string message = "Dear " + myReader["strCustDesc"].ToString() + ", your order of Kshs. " + myReader["decTotal"].ToString() + " will be delivered on " + expectedDate.ToString("dd-MM-yyyy") + ", Invoice No. " + myReader["strInvDocID"].ToString() + " with payment details indicated on the invoice. It is always a pleasure to serve you. Please call 0710752771 for any clarification. Thank you. ";

                                if (String.IsNullOrEmpty(myReader["strCellPhone"].ToString()))
                                {
                                    recipient = "";
                                }
                                else
                                {
                                    recipient = myReader["strCellPhone"].ToString();
                                }

                                try
                                {
                                    var newendpoint = "http://api.mspace.co.ke/mspaceservice/wr/sms/sendtext/username=SupaCoat/password=kenya5000/senderid=SupaCoatInv/recipient=" + recipient + "/message=" + message;
                                    var response = await client.GetAsync(newendpoint);
                                    //WriteToFile(response.ToString());
                                    var stringData = await response.Content.ReadAsStringAsync();
                                    //WriteToFile(stringData.ToString());
                                    if (response.IsSuccessStatusCode)
                                    {
                                        using (SqlConnection upcnn = new SqlConnection(constring))
                                        {
                                            SqlCommand upcmd = new SqlCommand("update tblInvoiceDoc set notificationsent=@notificationsent where strInvDocID=@strInvDocID");
                                            upcmd.CommandType = CommandType.Text;
                                            upcmd.Parameters.AddWithValue("@notificationsent", "1");
                                            upcmd.Parameters.AddWithValue("@strInvDocID", myReader["strInvDocID"].ToString());
                                            upcmd.Parameters.AddWithValue("@DateConsumed", DateTime.Now);
                                            upcnn.Open();
                                            upcmd.Connection = upcnn;
                                            upcmd.ExecuteNonQuery();
                                        }
                                        // WriteToFile("Update made successfully");
                                    }
                                }
                                catch (Exception ex)
                                {
                                  //  _logger.LogInformation(ex.Message, DateTimeOffset.Now);
                                }



                            }
                        }
                        catch (Exception ex)
                        {
                           // _logger.LogInformation(ex.Message, DateTimeOffset.Now);
                            continue;
                        }
                    }
                    myReader.Close();
                    cnn.Close();

                }

            }
            catch (Exception ex)
            {
                //_logger.LogInformation(ex.Message, DateTimeOffset.Now);
                // WriteToFile("Simple Service Error on: {0} " + ex.Message + ex.StackTrace);
            }

        }
        public async Task sendpaymentnotification()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            IConfiguration configuration = builder.Build();
            string constring = configuration.GetConnectionString("notifications");
            string? recipient;
            try
            {

                using (SqlConnection cnn = new SqlConnection(constring))
                {
                    SqlCommand cmd = new SqlCommand("select * from qrypayments where notificationsent=@notificationsent");
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@notificationsent", 0);
                    cmd.Parameters.AddWithValue("@DateConsumed", DateTime.Now);
                    cnn.Open();
                    cmd.Connection = cnn;
                    SqlDataReader myReader = cmd.ExecuteReader();
                    while (myReader.Read())
                    {
                        string? message;
                        DateTime transdate = DateTime.Parse(myReader["dteJournalDate"].ToString());
                        try
                        {

                            using (var client = new HttpClient())
                            {

                                if (myReader["strPaidBy"].ToString() == "Cheque")
                                {
                                    message = "Dear " + myReader["strCustDesc"].ToString() + ", we are pleased to inform you that we have banked your Cheque, No. " + myReader["strComment"].ToString() + " of Kshs " + myReader["decAmount"].ToString() + " on " + transdate.ToString("dd-MM-yyyy") + ". Thank you for doing business with us.Please call 0710752771 for any clarifications. ";
                                }
                                else
                                {

                                    message = "Dear " + myReader["strCustDesc"].ToString() + ", we are pleased to inform you that we have received a " + myReader["strPaidBy"].ToString() + " payment of " + myReader["decAmount"].ToString() + " on " + transdate.ToString("dd-MM-yyyy") + ". Thank you for doing business with us. Please call 0710752771 for any clarifications. ";

                                }
                                if (String.IsNullOrEmpty(myReader["strCellPhone"].ToString()))
                                {
                                    recipient = "";
                                }
                                else
                                {
                                    recipient = myReader["strCellPhone"].ToString();
                                }


                                if (String.IsNullOrEmpty(myReader["strCellPhone"].ToString()))
                                {
                                    recipient = "";
                                }
                                else
                                {
                                    recipient = myReader["strCellPhone"].ToString();
                                }

                                var newendpoint = "http://api.mspace.co.ke/mspaceservice/wr/sms/sendtext/username=SupaCoat/password=kenya5000/senderid=SupaCoatInv/recipient=" + recipient + "/message=" + message;
                                var response = await client.GetAsync(newendpoint);
                                var stringData = await response.Content.ReadAsStringAsync();

                                if (response.IsSuccessStatusCode)
                                {
                                    using (SqlConnection upcnn = new SqlConnection(constring))
                                    {
                                        SqlCommand upcmd = new SqlCommand("update tblCustPay set notificationsent=@notificationsent where lngCustPayID=@lngCustPayID");
                                        upcmd.CommandType = CommandType.Text;
                                        upcmd.Parameters.AddWithValue("@notificationsent", 1);
                                        upcmd.Parameters.AddWithValue("@lngCustPayID", myReader["lngCustPayID"].ToString());
                                        upcmd.Parameters.AddWithValue("@DateConsumed", DateTime.Now);
                                        upcnn.Open();
                                        upcmd.Connection = upcnn;
                                        upcmd.ExecuteNonQuery();
                                    }
                                }



                            }
                        }
                        catch (Exception ex)
                        {
                            //_logger.LogInformation(ex.Message, DateTimeOffset.Now);
                            //WriteToFile("Simple Service Error on: {0} " + ex.Message + ex.StackTrace);
                            continue;
                        }
                    }
                    myReader.Close();
                    cnn.Close();

                }

            }
            catch (Exception ex)
            {
                //_logger.LogInformation(ex.Message, DateTimeOffset.Now);
                //WriteToFile("Simple Service Error on: {0} " + ex.Message + ex.StackTrace);
            }

        }
    }
}
