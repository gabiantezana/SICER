﻿using MSS.TAWA.BC;
using MSS.TAWA.BE;
using MSS.TAWA.HP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Documento : System.Web.UI.Page
{
    private TipoDocumentoWeb _TipoDocumentoWeb { get; set; }
    private Modo _Modo { get; set; }
    private Int32 _IdDocumentoWeb { get; set; }

    #region On Load Page

    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["Usuario"] == null)
            Response.Redirect("~/Login.aspx");
        try
        {
            if (!this.IsPostBack)
            {

                //Get from context
                _TipoDocumentoWeb = (TipoDocumentoWeb)Context.Items[ConstantHelper.Keys.TipoDocumentoWeb];
                _Modo = (Modo)Context.Items[ConstantHelper.Keys.Modo];
                _IdDocumentoWeb = Convert.ToInt32(Context.Items[ConstantHelper.Keys.IdDocumentoWeb]);

                //Set to viewState
                ViewState[ConstantHelper.Keys.TipoDocumentoWeb] = _TipoDocumentoWeb;
                ViewState[ConstantHelper.Keys.Modo] = _Modo;
                ViewState[ConstantHelper.Keys.IdDocumentoWeb] = _IdDocumentoWeb;

                SetCrearOEditar();
            }
        }
        catch (Exception ex)
        {
            Mensaje("Ocurrió un error: " + ex.Message);
            ExceptionHelper.LogException(ex);
        }
    }

    private void SetCrearOEditar()
    {
        try
        {
            if (_TipoDocumentoWeb == TipoDocumentoWeb.Reembolso)
            {
                txtMontoInicial.Enabled = false;
                trEntregaRendir.Visible = true;
            }

            ListarUsuarioSolicitante();
            ListarMoneda();
            ListarEmpresa();

            switch (_Modo)
            {
                case Modo.Crear:
                    switch (_TipoDocumentoWeb)
                    {
                        case TipoDocumentoWeb.CajaChica:
                            lblCabezera.Text = "Crear Nueva Caja Chica";
                            break;
                        case TipoDocumentoWeb.EntregaRendir:
                            lblCabezera.Text = "Crear Nueva Entrega Rendir";
                            break;
                        case TipoDocumentoWeb.Reembolso:
                            lblCabezera.Text = "Crear Nuevo Reembolso";
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    LimpiarCampos();
                    break;
                case Modo.Editar:
                    switch (_TipoDocumentoWeb)
                    {
                        case TipoDocumentoWeb.CajaChica:
                            lblCabezera.Text = "Modificar Caja Chica";
                            break;
                        case TipoDocumentoWeb.EntregaRendir:
                            lblCabezera.Text = "Modificar Entrega Rendir";
                            break;
                        case TipoDocumentoWeb.Reembolso:
                            lblCabezera.Text = "Modificar Reembolso";
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    EditarDocumento_Fill();
                    break;
            }
            SetModadlidadBotones();
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(ex);
            Mensaje("Ocurrió un error: " + ex.Message);
        }
    }

    private void SetModadlidadBotones()
    {
        if (Session["Usuario"] == null)
            Response.Redirect("~/Login.aspx");
        else
        {
            switch (_Modo)
            {
                case Modo.Crear:
                    bCrear.Visible = true;
                    bCancelar.Visible = true;
                    bAprobar.Visible = false;
                    bRechazar.Visible = false;
                    bCancelar2.Visible = false;

                    ddlCentroCostos3.Enabled = false;
                    ddlCentroCostos4.Enabled = false;
                    ddlCentroCostos5.Enabled = false;
                    ddlIdMetodoPago.Enabled = false;
                    txtComentario.Enabled = false;
                    break;

                case Modo.Editar:
                    UsuarioBE objUsuarioSesionBE = new UsuarioBE();
                    objUsuarioSesionBE = (UsuarioBE)Session["Usuario"];
                    objUsuarioSesionBE = new UsuarioBC().ObtenerUsuario(objUsuarioSesionBE.IdUsuario, 0);

                    Boolean habilitarBotonesDeAprobacion = false;
                    Boolean habilitarBotonesDeGuardado = false;

                    DocumentoWebBE objDocumentoBE = new DocumentoWebBC().GetDocumentoWeb(_IdDocumentoWeb);
                    UsuarioBE objUsuarioSolicitanteBE = new UsuarioBC().ObtenerUsuario(objDocumentoBE.IdUsuarioSolicitante, 0);
                    PerfilUsuarioBE objPerfilUsuarioBE = new PerfilUsuarioBC().ObtenerPerfilUsuario(objUsuarioSesionBE.IdPerfilUsuario);

                    EstadoDocumento estadoDocumento = (EstadoDocumento)Enum.Parse(typeof(EstadoDocumento), objDocumentoBE.Estado);
                    TipoAprobador tipoAprobador = (TipoAprobador)Enum.Parse(typeof(TipoAprobador), objPerfilUsuarioBE.TipoAprobador);
                    switch (estadoDocumento)
                    {
                        //TODO: FALTA CONTEMPLAR NIVEL CONTADOR
                        case EstadoDocumento.PorAprobarNivel1:
                        case EstadoDocumento.PorAprobarNivel2:
                        case EstadoDocumento.PorAprobarNivel3:
                            switch (tipoAprobador)
                            {
                                case TipoAprobador.Aprobador:
                                case TipoAprobador.AprobadorYCreador:
                                    habilitarBotonesDeAprobacion = new ValidationHelper().UsuarioPuedeAprobarDocumento(estadoDocumento, objUsuarioSolicitanteBE, objUsuarioSesionBE.IdUsuario);
                                    break;
                            }
                            break;
                        case EstadoDocumento.Rechazado:
                            if (ddlIdUsuarioSolicitante.SelectedValue == objUsuarioSesionBE.IdUsuario.ToString())//Solo si el usuario ha creado el documento, puede modificarlo.
                                habilitarBotonesDeGuardado = true;
                            break;
                    }

                    bCrear.Visible = false;
                    bCancelar.Visible = false;
                    bAprobar.Visible = false;
                    bRechazar.Visible = false;
                    bCancelar2.Visible = false;
                    txtComentario.Enabled = false;

                    if (habilitarBotonesDeAprobacion)
                    {
                        txtComentario.Enabled = true;
                        bAprobar.Visible = true;
                        bRechazar.Visible = true;
                    }
                    if (habilitarBotonesDeGuardado)
                    {
                        bCrear.Visible = true;
                        bCrear.Text = "Guardar y enviar";
                    }
                    break;
            }
        }
    }

    private void LimpiarCampos()
    {
        txtIdDocumento.Text = "";
        txtCodigoDocumento.Text = "";
        txtAsunto.Text = "";
        txtMontoInicial.Text = "";
        txtComentario.Text = "";
    }

    private void EditarDocumento_Fill()
    {
        DocumentoWebBE objDocumentoBE = new DocumentoWebBC().GetDocumentoWeb(_IdDocumentoWeb);

        ListarCentroCostos(objDocumentoBE.IdEmpresa);
        ListarMetodosPago(objDocumentoBE.IdEmpresa);

        txtIdDocumento.Text = objDocumentoBE.IdDocumentoWeb.ToString();
        txtCodigoDocumento.Text = objDocumentoBE.CodigoDocumento;
        txtAsunto.Text = objDocumentoBE.Asunto;
        txtMontoInicial.Text = objDocumentoBE.MontoInicial.ToString();
        txtComentario.Text = objDocumentoBE.Comentario;
        txtMotivoDetalle.Text = objDocumentoBE.MotivoDetalle;

        ddlIdEmpresa.SelectedValue = objDocumentoBE.IdEmpresa.ToString();
        ddlIdUsuarioSolicitante.SelectedValue = objDocumentoBE.IdUsuarioSolicitante.ToString();
        ddlIdUsuarioSolicitante.Enabled = false;
        ddlMoneda.SelectedValue = objDocumentoBE.Moneda.ToString();

        if (objDocumentoBE.IdCentroCostos1 != null)
            ddlCentroCostos1.SelectedValue = objDocumentoBE.IdCentroCostos1.ToString();
        if (objDocumentoBE.IdCentroCostos2 != null)
            ddlCentroCostos2.SelectedValue = objDocumentoBE.IdCentroCostos2.ToString();
        if (objDocumentoBE.IdCentroCostos3 != null)
            ddlCentroCostos3.SelectedValue = objDocumentoBE.IdCentroCostos3.ToString();
        if (objDocumentoBE.IdCentroCostos4 != null)
            ddlCentroCostos4.SelectedValue = objDocumentoBE.IdCentroCostos4.ToString();
        if (objDocumentoBE.IdCentroCostos5 != null)
            ddlCentroCostos5.SelectedValue = objDocumentoBE.IdCentroCostos5.ToString();
        if (objDocumentoBE.IdMetodoPago != null)
            ddlIdMetodoPago.SelectedValue = objDocumentoBE.IdMetodoPago.ToString();

        if (_TipoDocumentoWeb == TipoDocumentoWeb.Reembolso)
        {
            trEntregaRendir.Visible = true;
            txtMontoInicial.Enabled = false;
        }
        else
        {
            trEntregaRendir.Visible = false;
            txtMontoInicial.Enabled = true;
        }
    }

    #endregion

    #region Listar Selects

    private void ListarUsuarioSolicitante()
    {
        try
        {
            UsuarioBC objUsuarioBC = new UsuarioBC();
            UsuarioBE objUsuarioBE = new UsuarioBE();
            List<UsuarioBE> lstUsuarioBE = new List<UsuarioBE>();

            if (_Modo == Modo.Crear)
            {
                objUsuarioBE = (UsuarioBE)Session["Usuario"];
                lstUsuarioBE = objUsuarioBC.ListarUsuario(0, objUsuarioBE.IdUsuario, 0);
            }
            else
            {
                DocumentoWebBE objDocumentoBE = new DocumentoWebBC().GetDocumentoWeb(_IdDocumentoWeb);
                lstUsuarioBE = objUsuarioBC.ListarUsuario(1, objDocumentoBE.IdUsuarioCreador, 0);
            }

            ddlIdUsuarioSolicitante.DataSource = lstUsuarioBE;
            ddlIdUsuarioSolicitante.DataTextField = "CardName";
            ddlIdUsuarioSolicitante.DataValueField = "IdUsuario";
            ddlIdUsuarioSolicitante.DataBind();
        }
        catch (Exception ex)
        {
            Mensaje("Ocurrió un error: " + ex.Message);
            ExceptionHelper.LogException(ex);
        }
    }

    private void ListarMoneda()
    {
        try
        {
            MonedaBC objMonedaBC = new MonedaBC();
            ddlMoneda.DataSource = objMonedaBC.ListarMoneda();
            ddlMoneda.DataTextField = "Descripcion";
            ddlMoneda.DataValueField = "IdMoneda";
            ddlMoneda.DataBind();
        }
        catch (Exception ex)
        {
            Mensaje("Ocurrió un error: " + ex.Message);
            ExceptionHelper.LogException(ex);
        }
    }

    private void ListarEmpresa()
    {
        try
        {
            EmpresaBC objEmpresaBC = new EmpresaBC();
            List<EmpresaBE> lstEmpresaBE = new List<EmpresaBE>();
            lstEmpresaBE = objEmpresaBC.ListarEmpresa();

            ddlIdEmpresa.DataSource = lstEmpresaBE;
            ddlIdEmpresa.DataTextField = "Descripcion";
            ddlIdEmpresa.DataValueField = "IdEmpresa";
            ddlIdEmpresa.DataBind();
        }
        catch (Exception ex)
        {
            Mensaje("Ocurrió un error: " + ex.Message);
            ExceptionHelper.LogException(ex);
        }
    }

    private void ListarCentroCostos(int idEmpresa)
    {
        var listCentroCostos = new CentroCostosBC().ListarCentroCostos(0);



        CentroCostosBC objCentroCostosBC = new CentroCostosBC();
        ddlCentroCostos1.DataSource = listCentroCostos.Where(x => x.Nivel == 1).ToList();
        ddlCentroCostos1.DataTextField = "Descripcion";
        ddlCentroCostos1.DataValueField = "IdCentroCostos";
        ddlCentroCostos1.DataBind();
        ddlCentroCostos1.Enabled = true;

        ddlCentroCostos2.DataSource = listCentroCostos.Where(x => x.Nivel == 2).ToList();
        ddlCentroCostos2.DataTextField = "Descripcion";
        ddlCentroCostos2.DataValueField = "IdCentroCostos";
        ddlCentroCostos2.DataBind();
        ddlCentroCostos2.Enabled = true;

        objCentroCostosBC = new CentroCostosBC();
        ddlCentroCostos3.DataSource = listCentroCostos.Where(x => x.Nivel == 3).ToList();
        ddlCentroCostos3.DataTextField = "Descripcion";
        ddlCentroCostos3.DataValueField = "IdCentroCostos";
        ddlCentroCostos3.DataBind();
        ddlCentroCostos3.Enabled = true;

        objCentroCostosBC = new CentroCostosBC();
        ddlCentroCostos4.DataSource = listCentroCostos.Where(x => x.Nivel == 4).ToList();
        ddlCentroCostos4.DataTextField = "Descripcion";
        ddlCentroCostos4.DataValueField = "IdCentroCostos";
        ddlCentroCostos4.DataBind();
        ddlCentroCostos4.Enabled = true;

        objCentroCostosBC = new CentroCostosBC();
        ddlCentroCostos5.DataSource = listCentroCostos.Where(x => x.Nivel == 5).ToList();
        ddlCentroCostos5.DataTextField = "Descripcion";
        ddlCentroCostos5.DataValueField = "IdCentroCostos";
        ddlCentroCostos5.DataBind();
        ddlCentroCostos5.Enabled = true;
    }

    private void ListarMetodosPago(int idEmpresa)
    {
        MetodoPagoBC objMetodoPagoBC = new MetodoPagoBC();
        ddlIdMetodoPago.DataSource = objMetodoPagoBC.ListarMetodoPago(idEmpresa, 1, 0);
        ddlIdMetodoPago.DataTextField = "Descripcion";
        ddlIdMetodoPago.DataValueField = "IdMetodoPago";
        ddlIdMetodoPago.DataBind();
        ddlIdMetodoPago.Enabled = true;
    }

    private void ListarEntregasRendir()
    {
        Int32 idUsuario = Convert.ToInt32(ddlIdUsuarioSolicitante.SelectedValue);
        String moneda = ddlMoneda.SelectedValue.ToString();
        ddlEntregaRendir.DataSource = new DocumentoWebBC().GetListRendicionesPendientesReembolso(idUsuario, moneda);
        ddlEntregaRendir.DataTextField = "CodigoDocumento";
        ddlEntregaRendir.DataValueField = "IdDocumentoWeb";
        ddlEntregaRendir.DataBind();
        ddlEntregaRendir.Enabled = true;

    }

    #endregion

    #region Submit Buttons

    public Boolean CamposSonValidos(out String errorMessage)
    {
        var indexNoValidos = new[] { "0", "-1" };
        errorMessage = String.Empty;

        if (indexNoValidos.Contains(ddlIdEmpresa.SelectedValue))
            errorMessage = "Debe ingresar la empresa";
        else if (indexNoValidos.Contains(ddlMoneda.SelectedValue))
            errorMessage = "Debe ingresar la  moneda";
        else if (String.IsNullOrWhiteSpace(txtMontoInicial.Text))
            errorMessage = "Debe ingresar el monto inicial";
        else if (!txtMontoInicial.Text.IsNumeric())
            errorMessage = "El importe inicial no es válido";
        else if (indexNoValidos.Contains(ddlCentroCostos1.SelectedValue))
            errorMessage = "Debe ingresar el centro de costo nivel 1";
        else if (String.IsNullOrWhiteSpace(txtAsunto.Text))
            errorMessage = "Debe ingresar el asunto.";
        else if (String.IsNullOrWhiteSpace(txtMotivoDetalle.Text))
            errorMessage = "Debe ingresar el motivo";
        else if (new ValidationHelper().UsuarioExcedeCantMaxDocumento(_TipoDocumentoWeb, Convert.ToInt32(ddlIdUsuarioSolicitante.SelectedItem.Value)))
            errorMessage = "El usuario ha excedido la cantidad máxima de documentos pendientes.";

        if (!String.IsNullOrEmpty(errorMessage))
            return false;
        else
            return true;
    }

    protected void Crear_Click(object sender, EventArgs e)
    {
        if (Session["Usuario"] == null)
            Response.Redirect("~/Login.aspx");

        try
        {
            _TipoDocumentoWeb = (TipoDocumentoWeb)ViewState[ConstantHelper.Keys.TipoDocumentoWeb];
            _Modo = (Modo)ViewState[ConstantHelper.Keys.Modo];

            _IdDocumentoWeb = Convert.ToInt32(ViewState[ConstantHelper.Keys.IdDocumentoWeb]);
            Int32 idUsuario = ((UsuarioBE)Session["Usuario"]).IdUsuario;

            String errorMessage;
            CamposSonValidos(out errorMessage);
            if (!String.IsNullOrEmpty(errorMessage))
            {
                Mensaje(errorMessage);
                return;
            }

            DocumentoWebBE objDocumentoBE = new DocumentoWebBE(_TipoDocumentoWeb);
            objDocumentoBE.IdDocumentoWeb = _IdDocumentoWeb;
            objDocumentoBE.CodigoDocumento = String.Empty;
            objDocumentoBE.IdUsuarioSolicitante = Convert.ToInt32(ddlIdUsuarioSolicitante.SelectedItem.Value);
            objDocumentoBE.IdEmpresa = Convert.ToInt32(ddlIdEmpresa.SelectedItem.Value);
            objDocumentoBE.IdCentroCostos1 = ddlCentroCostos1.SelectedItem.Value.ToString();
            objDocumentoBE.IdCentroCostos2 = ddlCentroCostos2.SelectedItem.Value.ToString();
            objDocumentoBE.IdCentroCostos3 = ddlCentroCostos3.SelectedItem.Value.ToString();
            objDocumentoBE.IdCentroCostos4 = ddlCentroCostos4.SelectedItem.Value.ToString();
            objDocumentoBE.IdCentroCostos5 = ddlCentroCostos5.SelectedItem.Value.ToString();
            objDocumentoBE.IdMetodoPago = 0;
            objDocumentoBE.IdArea = 0;
            objDocumentoBE.Asunto = txtAsunto.Text;
            objDocumentoBE.MontoInicial = Convert.ToDecimal(txtMontoInicial.Text);
            objDocumentoBE.MontoGastado = 0;
            objDocumentoBE.MontoActual = Convert.ToDecimal(txtMontoInicial.Text);
            objDocumentoBE.Moneda = Convert.ToInt32(ddlMoneda.SelectedItem.Value);
            objDocumentoBE.Comentario = null;
            objDocumentoBE.MotivoDetalle = txtMotivoDetalle.Text;
            objDocumentoBE.FechaSolicitud = DateTime.Now;
            objDocumentoBE.FechaContabilizacion = DateTime.Now;
            objDocumentoBE.Estado = EstadoDocumento.PorAprobarNivel1.IdToString();
            objDocumentoBE.IdUsuarioCreador = idUsuario;
            objDocumentoBE.UserCreate = idUsuario.ToString();
            objDocumentoBE.CreateDate = DateTime.Now;
            objDocumentoBE.UserUpdate = idUsuario.ToString();
            objDocumentoBE.UpdateDate = DateTime.Now;
            objDocumentoBE.Comentario = txtComentario.Text;

            if (ddlEntregaRendir.SelectedValue != null && ddlEntregaRendir.SelectedValue != "0" && ddlEntregaRendir.SelectedValue != "-1" && !string.IsNullOrEmpty(ddlEntregaRendir.SelectedValue))
                objDocumentoBE.IdDocumentoWebRendicionReferencia = Convert.ToInt32(ddlEntregaRendir.SelectedValue);

            new DocumentoWebBC().AddUpdateDocumento(objDocumentoBE);

            Response.Redirect("~/ListadoDocumentos.aspx?TipoDocumentoWeb=" + (Int32)_TipoDocumentoWeb);
        }
        catch (Exception ex)
        {
            Mensaje("Ocurrió un error: " + ex.Message);
            ExceptionHelper.LogException(ex);
        }
    }

    protected void Cancelar_Click(object sender, EventArgs e)
    {
        _TipoDocumentoWeb = (TipoDocumentoWeb)ViewState[ConstantHelper.Keys.TipoDocumentoWeb];
        Response.Redirect("~/ListadoDocumentos.aspx?TipoDocumentoWeb=" + (Int32)_TipoDocumentoWeb);
    }

    protected void Aprobar_Click(object sender, EventArgs e)
    {
        if (Session["Usuario"] == null)
            Response.Redirect("~/Login.aspx");

        try
        {
            _TipoDocumentoWeb = (TipoDocumentoWeb)ViewState[ConstantHelper.Keys.TipoDocumentoWeb];
            _Modo = (Modo)ViewState[ConstantHelper.Keys.Modo];
            _IdDocumentoWeb = (Int32)base.ViewState[ConstantHelper.Keys.IdDocumentoWeb];
            Int32 idUsuario = ((UsuarioBE)Session["Usuario"]).IdUsuario;

            CambioEstadoBE cambioEstadoBE = new CambioEstadoBE()
            {
                IdDocumentoWeb = _IdDocumentoWeb,
                Comentario = txtComentario.Text,
                IdUsuario = idUsuario
            };


            new DocumentoWebBC().AprobarDocumento(cambioEstadoBE);

            Response.Redirect("~/ListadoDocumentos.aspx?TipoDocumentoWeb=" + (Int32)_TipoDocumentoWeb);

        }
        catch (Exception ex)
        {
            Mensaje("Ocurrió un error: " + ex.Message);
            ExceptionHelper.LogException(ex);
        }
        finally
        {
            bAprobar.Enabled = true;
        }
    }

    protected void Rechazar_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(txtComentario.Text) || string.IsNullOrWhiteSpace(txtComentario.Text)) throw new Exception("Ingrese un comentario indicando el motivo de rechazo.");

            _TipoDocumentoWeb = (TipoDocumentoWeb)ViewState[ConstantHelper.Keys.TipoDocumentoWeb];
            _Modo = (Modo)ViewState[ConstantHelper.Keys.Modo];
            _IdDocumentoWeb = (Int32)base.ViewState[ConstantHelper.Keys.IdDocumentoWeb];
            Int32 idUsuario = ((UsuarioBE)Session["Usuario"]).IdUsuario;

            CambioEstadoBE cambioEstadoBE = new CambioEstadoBE()
            {
                IdDocumentoWeb = _IdDocumentoWeb,
                Comentario = txtComentario.Text,
                IdUsuario = idUsuario,
                TipoDocumentoOrigen = 1,
            };
            new DocumentoWebBC().RechazarDocumento(cambioEstadoBE);

            Response.Redirect("~/ListadoDocumentos.aspx?TipoDocumentoWeb=" + (Int32)_TipoDocumentoWeb);
        }
        catch (Exception ex)
        {
            Mensaje("Ocurrió un error: " + ex.Message);
            ExceptionHelper.LogException(ex);
        }
        finally
        {
            bRechazar.Enabled = true;
        }
    }

    private void Mensaje(String mensaje)
    {
        mensaje = mensaje.Replace("'", "");
        ScriptManager.RegisterClientScriptBlock(Page, this.GetType(), "MessageBox", "alert('" + mensaje + "')", true);
    }

    #endregion

    #region Select Change

    protected void ddlIdEmpresa_SelectedIndexChanged1(object sender, EventArgs e)
    {
        Int32 idEmpresa = Convert.ToInt32(ddlIdEmpresa.SelectedValue.ToString());
        if (idEmpresa != 0)
        {
            ddlCentroCostos1.Enabled = true;
            ddlCentroCostos2.Enabled = true;
            ddlCentroCostos3.Enabled = true;
            ddlCentroCostos4.Enabled = true;
            ddlCentroCostos5.Enabled = true;
            ddlIdMetodoPago.Enabled = true;

            ListarCentroCostos(idEmpresa);
            ListarMetodosPago(idEmpresa);
        }
        else
        {
            ddlCentroCostos1.SelectedValue = "0";
            ddlCentroCostos1.Enabled = false;
            ddlCentroCostos2.SelectedValue = "0";
            ddlCentroCostos2.Enabled = false;
            ddlCentroCostos3.SelectedValue = "0";
            ddlCentroCostos3.Enabled = false;
            ddlCentroCostos4.SelectedValue = "0";
            ddlCentroCostos4.Enabled = false;
            ddlCentroCostos5.SelectedValue = "0";
            ddlCentroCostos5.Enabled = false;

            ddlIdMetodoPago.SelectedValue = "0";
            ddlIdMetodoPago.Enabled = false;
        }
    }

    #endregion

    #region Validaciones

    private bool validaDecimales(string p)
    {
        string[] words = p.Split('.');
        int cantidad = words.Length;
        string decimales = "000";

        if (cantidad == 2) decimales = words[1];

        if (decimales.Length == 2) return true;
        else return false;
    }

    #endregion

    protected void ddlEntregaRendir_SelectedIndexChanged(object sender, EventArgs e)
    {
        Int32 codigo = Convert.ToInt32(ddlEntregaRendir.SelectedValue.ToString());
        Decimal num = new DocumentoWebBC().GetDocumentoWeb(codigo).MontoActual * -1;
        txtMontoInicial.Text = num.ToString();
    }

    protected void ddlIdUsuarioSolicitante_SelectedIndexChanged(object sender, EventArgs e)
    {
        ListarEntregasRendir();
    }

    protected void ddlMoneda_SelectedIndexChanged(object sender, EventArgs e)
    {
        ListarEntregasRendir();
    }
}