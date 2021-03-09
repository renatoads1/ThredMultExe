using System;
using System.Diagnostics;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Linq;

namespace ThredMultExe
{
    class Program
    {

        public static  string Strcon = ConfigurationManager.ConnectionStrings["MySQLConn"].ToString();
        static void Main(string[] args)
        {
            Thread t = new Thread(new ThreadStart(Munitora));
            t.Start();
        }

        public static void Munitora()
        {
            int horasel = int.Parse( ConfigurationManager.AppSettings.Get("HoraDownload"));
            int cont = 0;
            while (true)
            {
                Thread.Sleep(10000);
                string count = CheckStatus();
                //para que execute tem que ter linhas com Null e tem que ser status = 0
                string[] l = count.Split("*");
                int a = int.Parse(l[0]);
                int b = int.Parse(l[1]);
                //se estatus = 0 e tem null e fora de 9 horas 
                if ((a > 0) && (b > 0) && (DateTime.Now.Hour != horasel))
                {
                    try
                    {
                        cont = 0;
                        Console.WriteLine("0 = Executa Questor");
                        //prod
                        //Process.Start(@"C:\RENATOTESTE\Importacao_XML_Questor-FASE2\Importacao_XML_Questor-FASE2.EXE");
                        //teste
                        Process.Start(@"C:\Users\renatolacerda\source\repos\pbh-xml-prestados-FASE2\Importacao_XML_Questor\bin\Release\netcoreapp3.0\Importacao_XML_Questor-FASE2.exe");
                    }
                    catch (Exception e)
                    {

                        Console.WriteLine(e);
                    }

                } else if ((a == 0) && (b > 0) && (cont > 25) && (DateTime.Now.Hour != horasel)) {
                    //se estatus == 0 e tem NUll e count > 25 e fora de 9 horas esta travado
                    //ResolveTrava
                    Console.WriteLine("resolve trava");
                    ResolveTrava();
                    Console.WriteLine("update status 0");
                    UpdateStatus("0");
                    cont = 0;

                } else if ((a > 0) && (b == 0) && (DateTime.Now.Hour != horasel) && (ConfigurationManager.AppSettings.Get("HoraLog") == DateTime.Now.Hour.ToString())) {
                    //se status liberado não tem null fora de hora e logday != datetime(now) dispara log
                    DisparaLog();

                } else if ((DateTime.Now.Hour == horasel) && (a > 0) && (b == 0) ) {
                    //se ta na hora selenio status ta liberado não tem nulo sel hoje não foi exec
                    //exec selenio download
                    try
                    {   
                        UpdateStatus("1");
                        //robo
                        //Process.Start(@"C:\RENATOTESTE\dow\Importacao_XML_Questor.exe");
                        //local
                        Process.Start(@"C:\Users\renatolacerda\source\repos\pbh-xml-prestados\Importacao_XML_Questor\bin\Release\netcoreapp3.0\Importacao_XML_Questor.exe");
                    
                    }
                    catch (Exception erro)
                    {

                        Console.WriteLine(erro);
                    }

                }
                else
                {

                    cont++;
                    Console.WriteLine("1 = AGUARDANDO!!! -> " + cont.ToString());

                }

            }
        }

        public static void UpdateStatus(string status)
        {
            
            using (MySqlConnection conex = new MySqlConnection(Strcon))
            {
                string StrSql = "UPDATE RobosHomologacao.PBHXmlPrestQuestor set status = '"+status+"' where id = '1'";
                MySqlCommand comando = new MySqlCommand(StrSql, conex);
                conex.Open();
                int retorno = Convert.ToInt32(comando.ExecuteScalar());
            }
        }

        public static string CheckStatus()
        {
           
            using (MySqlConnection conex = new MySqlConnection(Strcon))
            {   
                string StrSql = "SELECT COUNT(*) FROM RobosHomologacao.PBHXmlPrestQuestor where status = '0'";
                string StrSql2 = "SELECT COUNT(*) FROM RobosHomologacao.PBHXmlPrestListFile where status = 'NULL'";
                MySqlCommand comando = new MySqlCommand(StrSql, conex);
                conex.Open();
                int count = Convert.ToInt32(comando.ExecuteScalar());
                MySqlCommand comando2 = new MySqlCommand(StrSql2, conex);
                int count2 = Convert.ToInt32(comando2.ExecuteScalar());
                return count.ToString() + "*" + count2.ToString();
            }
        }

        public static void ResolveTrava()
        {
            string t = "";
            string cnplalala ="";
            using (MySqlConnection conex = new MySqlConnection(Strcon))
            {
                Console.WriteLine("entrou linha 102");
                string StrSql3 = "SELECT * FROM RobosHomologacao.PBHXmlPrestListFile where status = 'NULL' Limit 1";
                MySqlCommand comando = new MySqlCommand(StrSql3, conex);
                conex.Open();

                MySqlDataReader rdr2 = comando.ExecuteReader();
                    while (rdr2.Read())
                    {                    
                        t = rdr2[0].ToString();
                    cnplalala = rdr2[2].ToString();
                    Console.WriteLine("entrou linha 111 id= "+t);
                    }
                    rdr2.Close();
                    conex.Close();
            }

            
            using (MySqlConnection conex5 = new MySqlConnection(Strcon))
            {
                string StrSql = "UPDATE RobosHomologacao.PBHXmlPrestListFile SET status = 'TRAVOU' WHERE id = '"+t+"';";
                Console.WriteLine("linha 129 update");
                MySqlCommand comando5 = new MySqlCommand(StrSql, conex5);
                conex5.Open();
                int retorno = Convert.ToInt32(comando5.ExecuteScalar());
                Console.WriteLine("linha 133 update ");
            }
            
            using (MySqlConnection conex6 = new MySqlConnection(Strcon))
            {
               
                string StrSql6 = "INSERT INTO RobosHomologacao.PBHXmlPrestLog(empresa, cnpj, mes_de_vigencia, sucesso, descricao, quantidade_de_nfs_baixadas, quantidade_de_nfs_processadas, tempo_execucao) VALUES('"+cnplalala+ "', '"+DateTime.Now.ToString()+"', '0', 'Erro Grave de exe questor travou!!!! checrar empresa com id tabela empresas "+t+" ','', '', '', '"+DateTime.Now.ToString()+"');";
                Console.WriteLine("Erro grave");
                MySqlCommand comando6 = new MySqlCommand(StrSql6, conex6);
                conex6.Open();
                int retorno6 = Convert.ToInt32(comando6.ExecuteScalar());
                Console.WriteLine("Erro Grave");
            }


                Process[] p = Process.GetProcessesByName("nfis");
                Console.WriteLine("Matando" + p.First().Id.ToString());
                var pid = p.First().Id;
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
                Process[] p2 = Process.GetProcessesByName("Importacao_XML_Questor-FASE2");
                Console.WriteLine("Matando" + p.First().Id.ToString());
                var pid2 = p2.First().Id;
                Process proc2 = Process.GetProcessById(pid2);
                proc2.Kill();
        }

        public static void  DisparaLog() {
            //se o log não foi disparado hoje dispara se nao = nada 
            

            using (MySqlConnection conexLog = new MySqlConnection(Strcon))
            {
                string StrSqlLog = "SELECT count(UltimoLogSend) FROM roboshomologacao.pbhxmlprestsendlog where id = 1 and UltimoLogSend = '"+DateTime.Now.ToString("dd/MM/yyyy") +"'; ";
                MySqlCommand comandoLog = new MySqlCommand(StrSqlLog, conexLog);
                conexLog.Open();
                int countLog = Convert.ToInt32(comandoLog.ExecuteScalar());
                if (countLog > 0)
                {
                    Console.WriteLine("data de hoje nao manda log"+ DateTime.Now.ToString("dd/MM/yyyy"));
                    Console.WriteLine("Aguardando FIla !!!");
                }
                else {
                    //data de ontem dispara log
                    try
                    {
                        //robo
                        //Process.Start(@"C:\RENATOTESTE\PLog\Importacao_XML_Questor.exe");
                        //local
                        Process.Start(@"C:\Users\renatolacerda\source\repos\SendLog\Importacao_XML_Questor\bin\Release\netcoreapp3.0\Importacao_XML_Questor.exe");
                        UpdateDataLog();
                    }
                    catch (Exception err)
                    {

                        Console.WriteLine(err); ;
                    }

                }
            }

        }
        public static void UpdateDataLog() {
            using (MySqlConnection conexUpdateDataLog = new MySqlConnection(Strcon))
            {
                string StrSqlUpdateDataLog = "UPDATE roboshomologacao.pbhxmlprestsendlog SET  UltimoLogSend = '"+DateTime.Now.ToString("dd/MM/yyyy") +"' WHERE id = '1';";
                MySqlCommand comandoUpdateDataLog = new MySqlCommand(StrSqlUpdateDataLog, conexUpdateDataLog);
                conexUpdateDataLog.Open();
                int retornoUpdateDataLog = Convert.ToInt32(comandoUpdateDataLog.ExecuteScalar());
            }
        }
    }
}
