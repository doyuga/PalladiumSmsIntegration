using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PalladiumSmsService
{
    public sealed class SmsService
    {
        private readonly ILogger<PalSmsService> _logger;
        private readonly string? _constring;

        public SmsService(ILogger<PalSmsService> logger, IOptions<ConnectionStrings> options)
        {
            _constring = options.Value.notifications;
            // _options = options.Value;
            _logger = logger;
        }
        public async Task sendinvoicenotification()
        {
            string? constring =_constring;
            string? recipient;
            string? message;
            string? invoiceid;
            try
            {

                using (SqlConnection cnn = new SqlConnection(constring))
                {
                    SqlCommand cmd = new SqlCommand("select * from qryinvoices where notificationsent=@notificationsent");
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@notificationsent", 0);
                    cmd.Parameters.AddWithValue("@DateConsumed", DateTime.Now);
                    cnn.Open();
                    cmd.Connection = cnn;
                    SqlDataReader myReader = cmd.ExecuteReader();
                    while (myReader.Read())
                    {
                        invoiceid = myReader["strInvDocID"].ToString();
                        try
                        {
                            using (var client = new HttpClient())
                            {
     
                                DateTime expectedDate = DateTime.Parse(myReader["dteRequired"].ToString());
                                string fstr= (myReader["strInvDocID"].ToString()).Substring(0, 2);
                                message = "nothing to send now";

                                if (System.String.IsNullOrEmpty(myReader["strCellPhone"].ToString()))
                                {
                                    recipient = "";
                                }
                                else
                                {
                                    recipient = myReader["strCellPhone"].ToString();
                                }
                                _logger.LogInformation(recipient + " now", DateTime.UtcNow);
                                if (fstr == "IN")
                                {
                                    message = "Dear " + myReader["strCustDesc"].ToString() + ", we have dispatched your order of " + Math.Round(Convert.ToDouble(myReader["decTotal"].ToString())) + " under " + myReader["strInvDocID"].ToString() + " to be delivered on " + expectedDate.ToString("dd-MM-yyyy") + " by " + myReader["strField1"].ToString();
                                    _logger.LogInformation("Application {applicationEvent} at {dateTime}", "Started", DateTime.UtcNow);
                                    _logger.LogInformation(message + " : messagge to be sent IN " + invoiceid, DateTime.UtcNow);
                                    await sendincn(message, recipient, invoiceid);


                                }
                                
                                else 
                                {
                                    message = "Dear " + myReader["strCustDesc"].ToString() + ", you have returned goods worth " + Math.Round(Convert.ToDouble(myReader["decTotal"].ToString())) + " which has been credited to your account.For clarifications, call 0710752771. ";
                                    _logger.LogInformation(message+"Application {applicationEvent} at {dateTime}", "Started", DateTime.UtcNow);
                                    _logger.LogInformation(message + " : messagge to be sent CN " + invoiceid, DateTime.UtcNow);
                                    await sendincn(message, recipient,invoiceid);
                                }
                      

                                

                            }
                        }
                        catch (Exception ex)
                        {
                             _logger.LogInformation(ex.Message, DateTimeOffset.Now);
                            continue;
                        }
                    }
                    myReader.Close();
                    cnn.Close();

                }

            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message, DateTimeOffset.Now);
                
            }

        }
        public async Task sendpaymentnotification()
        {

            string? constring = _constring;
            string? recipient;
            string? payid;
            string? message;
            try
            {

                using (SqlConnection cnn = new SqlConnection(constring))
                {
                    SqlCommand cmd = new SqlCommand("select * from qrypayments where notificationsent=@notificationsent and decBankAmount>@decBankAmount");
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@notificationsent", 0);
                    cmd.Parameters.AddWithValue("@decBankAmount", 0);
                    cmd.Parameters.AddWithValue("@DateConsumed", DateTime.Now);
                    cnn.Open();
                    cmd.Connection = cnn;
                    SqlDataReader myReader = cmd.ExecuteReader();
                    while (myReader.Read())
                    {
                        
                        DateTime transdate = DateTime.Parse(myReader["dteJournalDate"].ToString());
                        DateTime today = DateTime.Now;
                        try
                        {

                            using (var client = new HttpClient())
                            {


                                if (Convert.ToDouble(myReader["decBankAmount"].ToString()) > 0)
                                {

                                    if (System.String.IsNullOrEmpty(myReader["strCellPhone"].ToString()))
                                    {
                                        continue;
                                    }
 
                                        recipient = myReader["strCellPhone"].ToString();
                                        payid = myReader["lngCustPayID"].ToString();
                                        DateTime jondate = DateTime.Parse(myReader["dteJournalDate"].ToString());
                                        
                                        if (myReader["strPaidBy"].ToString() == "Cheque")
                                        {
                                            if (transdate.ToString("d")!=today.ToString("d"))
                                            {
                                                continue;
                                            }


                                            message = "Dear " + myReader["strCustDesc"].ToString() + ", we are pleased to inform you that we will be banking your Cheque, No. " + myReader["strComment"].ToString() + " of Kshs " + Math.Round(Convert.ToDouble(myReader["decAmount"].ToString())) + " today. For clarifications, call 0710752771.Thank you.  ";
                                            await sendchequesms(message, recipient, payid);
                                        }
                                        else
                                        {
                                            message = "Dear " + myReader["strCustDesc"].ToString() + ", we have received your " + myReader["strPaidBy"].ToString() + " payment of " + Math.Round(Convert.ToDouble(myReader["decAmount"].ToString()), 2) + " on " + transdate.ToString("dd-MM-yyyy") + ". Thank you for doing business with us. ";
                                            await sendpaymentsms(message, recipient, payid);
                                            message = "Dear " + myReader["strCustDesc"].ToString() + ", you have an outstanding balance of " + Math.Round(Convert.ToDouble(myReader["decBalDue"].ToString())) + ". Only use the payment details indicated on the invoice.Please call 0710752771 to get a detailed statement. ";
                                            await sendbalancesms(message, recipient, payid);


                                            
                                        }


                                    
                                }
              

                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation(ex.Message, DateTimeOffset.Now);
                            continue;
                        }
                    }
                    myReader.Close();
                    cnn.Close();

                }

            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message, DateTimeOffset.Now);

            }

        }
        public async Task senddebitnotesms(string message,string recepient, string custpayid )
        {
            string? constring = _constring;
            try
            {
                using (var client = new HttpClient())
                {



                    var newendpoint = "http://api.mspace.co.ke/mspaceservice/wr/sms/sendtext/username=SupaCoat/password=kenya5000/senderid=SupaCoatInv/recipient=" + recepient + "/message=" + message;
                    var response = await client.GetAsync(newendpoint);
                    var stringData = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        using (SqlConnection upcnn = new SqlConnection(constring))
                        {
                            SqlCommand upcmd = new SqlCommand("update tblCustPay set creditnotificationsent=@creditnotificationsent where lngCustPayID=@lngCustPayID");
                            upcmd.CommandType = CommandType.Text;
                            upcmd.Parameters.AddWithValue("@creditnotificationsent", 1);
                            upcmd.Parameters.AddWithValue("@lngCustPayID", custpayid);
                            upcmd.Parameters.AddWithValue("@DateConsumed", DateTime.Now);
                            upcnn.Open();
                            upcmd.Connection = upcnn;
                            upcmd.ExecuteNonQuery();
                        }
                    }



                }

            }
            catch(Exception ex) 
            {
                _logger.LogInformation(ex.Message.ToString(), DateTimeOffset.Now);
            }
        }
        public async Task sendbalancesms(string message, string recepient, string custpayid)
        {
            string? constring = _constring;
            try
            {
                using (var client = new HttpClient())
                {



                    var newendpoint = "http://api.mspace.co.ke/mspaceservice/wr/sms/sendtext/username=SupaCoat/password=kenya5000/senderid=SupaCoatInv/recipient=" + recepient + "/message=" + message;
                    var response = await client.GetAsync(newendpoint);
                    var stringData = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        using (SqlConnection upcnn = new SqlConnection(constring))
                        {
                            SqlCommand upcmd = new SqlCommand("update tblCustPay set balancenotificationsent=@balancenotificationsent where lngCustPayID=@lngCustPayID");
                            upcmd.CommandType = CommandType.Text;
                            upcmd.Parameters.AddWithValue("@balancenotificationsent", 1);
                            upcmd.Parameters.AddWithValue("@lngCustPayID", custpayid);
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
                _logger.LogInformation(ex.Message.ToString(), DateTimeOffset.Now);
            }
        }
        public async Task sendchequesms(string message, string recepient, string custpayid)
        {
            string? constring = _constring;
            try
            {
                using (var client = new HttpClient())
                {



                    var newendpoint = "http://api.mspace.co.ke/mspaceservice/wr/sms/sendtext/username=SupaCoat/password=kenya5000/senderid=SupaCoatInv/recipient=" + recepient + "/message=" + message;
                    var response = await client.GetAsync(newendpoint);
                    var stringData = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        using (SqlConnection upcnn = new SqlConnection(constring))
                        {
                            SqlCommand upcmd = new SqlCommand("update tblCustPay set chequenotificationsent=@chequenotificationsent where lngCustPayID=@lngCustPayID");
                            upcmd.CommandType = CommandType.Text;
                            upcmd.Parameters.AddWithValue("@notificationsent", 1);
                            upcmd.Parameters.AddWithValue("@lngCustPayID", custpayid);
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
                _logger.LogInformation(ex.Message.ToString(), DateTimeOffset.Now);
            }
        }
        public async Task sendpaymentsms(string message, string recepient, string custpayid)
        {
            string? constring = _constring;
            try
            {
                using (var client = new HttpClient())
                {



                    var newendpoint = "http://api.mspace.co.ke/mspaceservice/wr/sms/sendtext/username=SupaCoat/password=kenya5000/senderid=SupaCoatInv/recipient=" + recepient + "/message=" + message;
                    var response = await client.GetAsync(newendpoint);
                    var stringData = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        using (SqlConnection upcnn = new SqlConnection(constring))
                        {
                            SqlCommand upcmd = new SqlCommand("update tblCustPay set notificationsent=@notificationsent where lngCustPayID=@lngCustPayID");
                            upcmd.CommandType = CommandType.Text;
                            upcmd.Parameters.AddWithValue("@notificationsent", 1);
                            upcmd.Parameters.AddWithValue("@lngCustPayID", custpayid);
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
                _logger.LogInformation(ex.Message.ToString(), DateTimeOffset.Now);
            }
        }
        public async Task sendincn(string message, string recepient, string invoiceid)
        {
            string? constring = _constring;
            try
            {
                using (var client = new HttpClient())
                {
                    _logger.LogInformation(message + " : messagge to be sent", DateTime.UtcNow);
                    //var newendpoint = "http://api.mspace.co.ke/mspaceservice/wr/sms/sendtext/username=SupaCoat/password=kenya5000/senderid=SupaCoatInv/recipient=" + recipient + "/message=" + message;
                    var newendpoint = "http://api.mspace.co.ke/mspaceservice/wr/sms/sendtext/username=SupaCoat/password=kenya5000/senderid=SupaCoatInv/recipient=" + recepient + "/message=" + message;
                    var response = await client.GetAsync(newendpoint);
                    //WriteToFile(response.ToString());
                    var stringData = await response.Content.ReadAsStringAsync();
                    string respdata = stringData.ToString();
                    _logger.LogInformation(respdata + " : response data for invoice", DateTime.UtcNow);
                    //WriteToFile(stringData.ToString());
                    if (response.IsSuccessStatusCode)
                    {
                        using (SqlConnection upcnn = new SqlConnection(constring))
                        {
                            SqlCommand upcmd = new SqlCommand("update tblInvoiceDoc set notificationsent=@notificationsent where strInvDocID=@strInvDocID");
                            upcmd.CommandType = CommandType.Text;
                            upcmd.Parameters.AddWithValue("@notificationsent", 1);
                            upcmd.Parameters.AddWithValue("@strInvDocID", invoiceid);
                            upcmd.Parameters.AddWithValue("@DateConsumed", DateTime.Now);
                            upcnn.Open();
                            upcmd.Connection = upcnn;
                            upcmd.ExecuteNonQuery();
                        }
                        _logger.LogInformation(invoiceid+ " : update successful", DateTime.UtcNow);
                    }
                    else
                    {
                        _logger.LogInformation(" error sending message: " + respdata + " : response data for invoice", DateTime.UtcNow);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("exception error while sending invoice message:" + ex.Message, DateTimeOffset.Now);
            }
        }
    }
}
