using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Servidor
{
    /// <summary>
    /// Servidor de un chat multiusuario.
    /// </summary>
    public partial class frmServidor : Form
    {
        #region [ Variables ]
        /// <summary>
        /// Esta clase permite hacer las solicitudes de conexión 
        /// </summary>
        private TcpListener servidor;
        /// <summary>
        /// Guarda el nombre de los clientes que están conectados
        /// </summary>
        private static Hashtable clientes_conectados;
        /// <summary>
        /// Mensaje entre clientes
        /// </summary>
        private String mensajeCliente;
        /// <summary>
        /// Mensaje que se envía a la pantalla del Servidor.
        /// </summary>
        private String _mensajeChat;
        /// <summary>
        /// Variable boolean que activa o desactiva el boton de inicio
        /// </summary>
        private Boolean btnActivar = true;
        /// <summary>
        /// Variable para el manejo del id del hilo que maneja el servidor
        /// </summary>
        private int idThread;
        /// <summary>
        /// Definimos la variable a utilizar en la creacion de los hilos
        /// </summary>
        Thread InitServer;
        #endregion

        #region [ Constructor ]
        public frmServidor()
        {
            InitializeComponent();
        }
        #endregion

        #region [ Eventos ]
        /// <summary>
        /// Método para inicia el servidor
        /// </summary>
        private void btnIniciar_Click(object sender, EventArgs e)
        {
            if (btnActivar)
            {
                btnActivar = false;
                btnIniciar.Enabled = false;
                btnDetener.Enabled = true;
                InitServer = new Thread(metodoIniciarServidor);
                InitServer.Start();
                
            }
        }

 
        /// <summary>
        /// Detiene el servidor
        /// </summary>
        private void btnDetener_Click(object sender, EventArgs e)
        {
            if (servidor != null && !servidor.Server.Connected)
            {
                btnActivar = true;
                btnIniciar.Enabled = false;
                btnDetener.Enabled = true;
                servidor.Stop();
             }
        }
        #endregion

        #region [ Métodos ]
        /// <summary>
        /// Envia elvmensaje de un usuario a los demas usuarios conectados.
        /// </summary>
        /// <param name="mensaje">Mensaje del Usuario</param>
        /// <param name="nombre">Nombre del usuario</param>
        /// <param name="flag">Bandera que se utiliza para determinar si se agrega el texto 
        /// "dice" al mensaje enviado por el usuario</param>
        /// 

        public void metodoIniciarServidor()
        {
            try
            {
                //Se inicia la ventana de actividades del chat
                clientes_conectados = new Hashtable();
                //Se inicia un nuevo servidor en la IP y puerto definidos.
                servidor = new TcpListener(IPAddress.Parse(txtIP.Text), int.Parse(txtPuerto.Text));
                //Se inicia el servidor
                servidor.Start();
                //Se muestra mensaje indicando que el servidor esta listo para aceptar solicitudes de los clientes
                _mensajeChat = "El servidor se ha iniciado";
                Mensaje();
                //Definición de los clientes
                TcpClient cliente;
                //Se cumple con la restricción de que se reciban mensajes de solo 160 caracteres
                Byte[] bytesCliente = new Byte[160];
                NetworkStream streamCliente;
                Chat chat;
                while (true)
                {
                    //Acepta un nuevo cliente
                    cliente = servidor.AcceptTcpClient();
                    //Se lee el mensaje del cliente
                    streamCliente = cliente.GetStream();
                    //bytesCliente sólo acepta mensajes de máximo 160 caracteres
                    streamCliente.Read(bytesCliente, 0, bytesCliente.Length);
                    //Traducimos el mensage a un string en codificación ASCII
                    mensajeCliente = Encoding.ASCII.GetString(bytesCliente, 0, bytesCliente.Length);
                    mensajeCliente = mensajeCliente.Substring(0, mensajeCliente.IndexOf("$"));
                    //Verificamos si ya existe ese nombre de usuario.                     
                    if (!clientes_conectados.ContainsKey(mensajeCliente))
                    {
                        //Realmente no hacemos ninguna validación posterior ni se manda un mensaje al 
                        //usuario pero por funcionalidad agregaremos esta condición.
                        clientes_conectados.Add(mensajeCliente, cliente);
                        _mensajeChat = string.Format("{0} se ha unido al servidor", mensajeCliente);
                        Mensaje();
                    }
                    else
                    {
                      MessageBox.Show("El usuario ya existe, intente de nuevo");
                       //Cliente.Stop();


                                                                  
                   }

                    //Mandamos el mensaje a todos los clientes el mensaje
                    DifundirATodos(mensajeCliente, mensajeCliente, false);

                    //Ciclamos el proceso para que se quede el servidor en espera de nuevas conexiones o mensajes
                    chat = new Chat(cliente, mensajeCliente);
                }
            }
            catch (Exception ex)
            {
                _mensajeChat = ex.ToString();
                Mensaje();
            }
            finally
            {
                if(servidor!=null)
                servidor.Stop();
            }
        }


        public static void DifundirATodos(string mensaje, string nombre, bool flag)
        {
            try
            {
                //Por cada cliente
                foreach (DictionaryEntry Item in clientes_conectados)
                {
                    Byte[] bytes = null;

                    TcpClient cliente;
                    cliente = (TcpClient)Item.Value;

                    NetworkStream streamCliente = cliente.GetStream();


                    if (flag == true)
                        bytes = Encoding.ASCII.GetBytes(nombre + " dice : " + mensaje);
                    else
                        bytes = Encoding.ASCII.GetBytes(nombre + " se ha conectado");

                    //transmitimos el mensaje
                    streamCliente.Write(bytes, 0, bytes.Length);
                    streamCliente.Flush();
                }
            }
            catch (Exception e)
            {
                //MessageBox.Show(" error " + e);
            }
        }

        public void Mensaje()
        {
            
            if (this.InvokeRequired)
                this.Invoke(new MethodInvoker(Mensaje));
            else
                txtChat.Text = txtChat.Text + Environment.NewLine + " -> " + _mensajeChat;
        }
        #endregion

        private void txtIP_TextChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }

    public class Chat
    {
        #region [ Variables ]
        TcpClient clienteChat;
        string nombreUsuario;
        #endregion        

        public Chat(TcpClient cliente, string nUsuario)
        {
            clienteChat = cliente;
            nombreUsuario = nUsuario;
            //Iniciamos un nuevo proceso que cicle la espera de mensajes nuevos por parte de los clientes.
            Thread ctThread = new Thread(doChat);
            ctThread.Start();
        }

        /// <summary>
        /// Cicla el proceso indefinidamente para que el servidor quede a la espera de nuevos mensajes 
        /// por parte de los clientes.
        /// </summary>
        private void doChat()
        {
            byte[] bytesFrom = new byte[256];
            string mensajeCliente = null;

            while (true)
            {
                try
                {
                    NetworkStream networkStream = clienteChat.GetStream();

                    networkStream.Read(bytesFrom, 0, bytesFrom.Length);
                    mensajeCliente = System.Text.Encoding.ASCII.GetString(bytesFrom);
                    mensajeCliente = mensajeCliente.Substring(0, mensajeCliente.IndexOf("$"));
                    //txtMensajes.Text += "From client - " + clNo + " : " + dataFromClient;

                    //Difundimos el mensaje a todos los clientes
                    frmServidor.DifundirATodos(mensajeCliente, nombreUsuario, true);
                }
                catch (Exception)
                {
                    //TODO: Manejar Excepción                
                }
            }
        }
    }
}
