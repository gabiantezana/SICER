﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Windows.Forms;

using MSS.TAWA.BC;
using MSS.TAWA.BE;
using System.Net.Mail;
using System.Data;
using System.IO;
using System.Web.UI.HtmlControls;
using System.Text;

using System.Net;
using System.Net.NetworkInformation;
using System.Globalization;
using MSS.TAWA.HP;
using MSS.TAWA.MODEL;

public partial class DocumentoRendicion : System.Web.UI.Page
{
    TipoDocumentoWeb _TipoDocumentoWeb;
    Modo _Modo;
    Int32 _IdDocumentoWeb;

    #region OnLoad Page

    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["Usuario"] == null)
            Response.Redirect("~/Login.aspx");

        try
        {
            ScriptManager scripManager = ScriptManager.GetCurrent(this.Page);
            scripManager.RegisterPostBackControl(lnkExportarReporte);

            if (!this.IsPostBack)
            {
                _TipoDocumentoWeb = (TipoDocumentoWeb)Context.Items[ConstantHelper.Keys.TipoDocumentoWeb];
                _Modo = (Modo)Context.Items[ConstantHelper.Keys.Modo];
                _IdDocumentoWeb = Convert.ToInt32(Context.Items[ConstantHelper.Keys.IdDocumentoWeb].ToString());

                ViewState[ConstantHelper.Keys.TipoDocumentoWeb] = _TipoDocumentoWeb;
                ViewState[ConstantHelper.Keys.Modo] = _Modo;
                ViewState[ConstantHelper.Keys.IdDocumentoWeb] = _IdDocumentoWeb;

                ListarTipoDocumento();
                //ListarProveedor();
                ListarProveedorCrear();
                ListarCentroCostos();
                ListarConcepto();
                ListarRendicion();
                ListarMoneda(_IdDocumentoWeb);
                Modalidad(_Modo);
                SetModalidadBotones(_Modo, _IdDocumentoWeb);
                ListarCuentasContablesDevoluciones();
                ListarPartidasPresupuestales(String.Empty);

                DocumentoWebBE objDocumento = new DocumentoWebBC().GetDocumentoWeb(_IdDocumentoWeb);

                if (objDocumento.Estado == "19") //TODO: ESTADOS
                    txtFechaContabilizacion.Text = (objDocumento.FechaContabilizacion).ToString("dd/MM/yyyy");
                else
                    txtFechaContabilizacion.Text = (DateTime.Today).ToString("dd/MM/yyyy");

                txtComentario.Text = objDocumento.Comentario;

                if ((EmpresaInterna)Session[ConstantHelper.Keys.IdEmpresaInterna] == EmpresaInterna.IIMP)
                {
                    TrPartidaPresupuestal.Attributes.Add("style", "display:normal");
                }

            }
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(ex);
            Mensaje("Ocurrió un error::" + ex.Message);
        }
    }

    private void Modalidad(Modo modo)
    {
        try
        {
            switch (modo)
            {
                case Modo.Crear:
                    LlenarCabecera();
                    LimpiarCampos();
                    break;
                case Modo.Editar:
                    //lblCabezera.Text = "Aprobar Caja Chica";
                    //bCrear.Text = "Guardar";
                    //LlenarCampos(Convert.ToInt32(ViewState[ConstantHelper.Keys.IdDocumentoWeb].ToString()));
                    break;
            }
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(ex);
            Mensaje("Ocurrió un error: " + ex.Message);
        }
    }

    private void SetModalidadBotones(Modo modo, int IdDocumento)
    {
        if (Session["Usuario"] == null)
            Response.Redirect("~/Login.aspx");

        Int32 IdUsuario = ((UsuarioBE)Session["Usuario"]).IdUsuario;
        UsuarioBE objUsuarioSesionBE = new UsuarioBC().ObtenerUsuario(IdUsuario, 0);
        PerfilUsuarioBE objPerfilUsuarioBE = new PerfilUsuarioBC().ObtenerPerfilUsuario(objUsuarioSesionBE.IdPerfilUsuario);
        TipoAprobador TipoAprobador = (TipoAprobador)Enum.Parse(typeof(TipoAprobador), new PerfilUsuarioBC().ObtenerPerfilUsuario(objUsuarioSesionBE.IdPerfilUsuario).TipoAprobador);

        DocumentoWebBE objDocumentoBE = new DocumentoWebBC().GetDocumentoWeb(IdDocumento);
        UsuarioBE objUsuarioSolicitanteBE = new UsuarioBC().ObtenerUsuario(objDocumentoBE.IdUsuarioSolicitante, 0);
        EstadoDocumento EstadoDocumento = (EstadoDocumento)Enum.Parse(typeof(EstadoDocumento), objDocumentoBE.Estado);

        Boolean setAsCreation = false;
        Boolean setAsAprobar = false;
        Boolean setAsContabilidad = false;
        bool setAsUpdate = false;

        switch (EstadoDocumento)
        {
            case EstadoDocumento.Aprobado:
            case EstadoDocumento.RendirAprobado:
            case EstadoDocumento.Rechazado:
                switch (TipoAprobador)
                {
                    case TipoAprobador.Creador:
                    case TipoAprobador.AprobadorYCreador:
                    case TipoAprobador.ContabilidadYCreador:
                        setAsCreation = true;
                        break;
                }
                break;
            case EstadoDocumento.RendirPorAprobarJefeArea:
                switch (TipoAprobador)
                {
                    case TipoAprobador.Aprobador:
                    case TipoAprobador.AprobadorYCreador:
                        if (objUsuarioSolicitanteBE.IdUsuarioCC1 == objUsuarioSesionBE.IdUsuario)
                            setAsAprobar = true;
                        break;
                }
                break;
            case EstadoDocumento.RendirPorAprobarContabilidad:
                switch (TipoAprobador)
                {
                    case TipoAprobador.Contabilidad:
                    case TipoAprobador.ContabilidadYCreador:
                        setAsContabilidad = true;
                        break;
                }
                break;
            case EstadoDocumento.RendirRechazado:
                if (objUsuarioSolicitanteBE.IdUsuario == objUsuarioSesionBE.IdUsuario)
                    setAsUpdate = true;
                break;
            default:
                break;
        }

        //Set all in false
        gvDocumentos.Columns[0].Visible = false;
        gvDocumentos.Columns[1].Visible = false;
        gvDocumentos.Columns[2].Visible = false;
        bAgregar.Visible = false;
        bGuardar.Visible = false;
        bCancelar.Visible = true;
        bMasivo.Visible = false;
        bAgregar2.Visible = false;
        bGuardar2.Visible = false;
        lblComentario.Visible = true;
        txtComentario.Visible = true;
        bEnviar.Visible = false;
        bAprobar.Visible = false;
        bLiquidar.Visible = false;
        bRechazar.Visible = false;

        if (setAsCreation)
        {

            gvDocumentos.Columns[1].Visible = true;
            gvDocumentos.Columns[2].Visible = true;
            bAgregar.Visible = true;
            bCancelar.Visible = true;
            bMasivo.Visible = true;
            bAgregar2.Visible = true;
            bEnviar.Visible = true;
        }

        if (setAsUpdate)
        {
            gvDocumentos.Columns[1].Visible = true;
            gvDocumentos.Columns[2].Visible = true;
            bAgregar.Visible = true;
            bCancelar.Visible = true;
            bMasivo.Visible = true;
            bAgregar2.Visible = true;
            bEnviar.Visible = true;
        }

        if (setAsAprobar)
        {
            gvDocumentos.Columns[1].Visible = true;
            gvDocumentos.Columns[2].Visible = true;
            bAgregar.Visible = true;
            bCancelar.Visible = true;
            bMasivo.Visible = true;
            bAgregar2.Visible = true;
            bAprobar.Visible = true;
            bRechazar.Visible = true;
        }

        if (setAsContabilidad)
        {
            gvDocumentos.Columns[0].Visible = true;
            gvDocumentos.Columns[1].Visible = true;
            gvDocumentos.Columns[2].Visible = true;
            bAgregar.Visible = true;
            bCancelar.Visible = true;
            bMasivo.Visible = true;
            bAgregar2.Visible = true;
            bAprobar.Visible = true;
            bLiquidar.Visible = true;
            txtFechaContabilizacion.Enabled = true;
            bRechazar.Visible = true;
        }
    }

    #endregion

    #region Listar Selects

    private void ListarTipoDocumento()
    {
        try
        {
            DocumentoBC objDocumentoBC = new DocumentoBC();
            ddlTipoDocumentoWeb.DataSource = objDocumentoBC.ListarDocumento(0, 0);
            ddlTipoDocumentoWeb.DataTextField = "Descripcion";
            ddlTipoDocumentoWeb.DataValueField = "IdDocumento";
            ddlTipoDocumentoWeb.DataBind();
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(ex);
            Mensaje("Ocurrió un error: " + ex.Message);
        }
    }

    private void ListarProveedor()
    {
        try
        {
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(ex);
            Mensaje("Ocurrió un error: " + ex.Message);
        }
    }

    private void ListarCentroCostos()
    {
        try
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
        catch (Exception ex)
        {
            ExceptionHelper.LogException(ex);
            Mensaje("Ocurrió un error: " + ex.Message);
        }
    }

    private void ListarConcepto()
    {
        try
        {
            ConceptoBC objConceptoBC = new ConceptoBC();
            ddlConcepto.DataSource = objConceptoBC.ListarConcepto();
            ddlConcepto.DataTextField = "Descripcion";
            ddlConcepto.DataValueField = "IdConcepto";
            ddlConcepto.DataBind();
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(ex);
            Mensaje("Ocurrió un error: " + ex.Message);
        }
    }

    private void ListarRendicion()
    {
        Int32 idDocumento = Convert.ToInt32(ViewState[ConstantHelper.Keys.IdDocumentoWeb].ToString());

        gvDocumentos.DataSource = new DocumentoWebBC().GetList(idDocumento, true);
        gvDocumentos.DataBind();

    }

    private void ListarMoneda(int IdDocumento)
    {
        MonedaBC objMonedaBC = new MonedaBC();

        ddlIdMonedaOriginal.DataSource = objMonedaBC.ListarMoneda(IdDocumento);
        ddlIdMonedaOriginal.DataTextField = "Descripcion";
        ddlIdMonedaOriginal.DataValueField = "IdMoneda";
        ddlIdMonedaOriginal.DataBind();
        ddlIdMonedaOriginal.SelectedValue = new DocumentoWebBC().GetDocumentoWeb(_IdDocumentoWeb).Moneda.ToString();

        ddlIdMonedaDoc.DataSource = objMonedaBC.ListarMoneda();//.Where(x => x.IdMoneda.ToString() == ddlIdMonedaOriginal.Text).ToList();
        ddlIdMonedaDoc.DataTextField = "Descripcion";
        ddlIdMonedaDoc.DataValueField = "IdMoneda";
        ddlIdMonedaDoc.DataBind();

    }

    private void ListarProveedorCrear()
    {
        String iddocumento = ViewState[ConstantHelper.Keys.IdDocumentoWeb].ToString();

        ProveedorBC objProveedorBC = new ProveedorBC();
        gvProveedor.DataSource = objProveedorBC.ListarProveedor(Convert.ToInt32(iddocumento), 2);
        gvProveedor.DataBind();
    }

    private void ListarCuentasContablesDevoluciones()
    {
        ddlCuentaContableDevolucion.DataSource = new CuentaContableBC().GetCuentasContables();
        ddlCuentaContableDevolucion.DataTextField = "U_Descripcion";
        ddlCuentaContableDevolucion.DataValueField = "U_Codigo";
        ddlCuentaContableDevolucion.DataBind();
    }

    private void ListarPartidasPresupuestales(String codigoCentroCostos)
    {
        Int32 IdEmpresaEnterna = Convert.ToInt32(ConfigurationManager.AppSettings[ConstantHelper.Keys.IdEmpresaInterna].ToString());
        if ((EmpresaInterna)IdEmpresaEnterna == EmpresaInterna.IIMP)
        {
            ddlPartidaPresupuestal.DataSource = new PartidaPresupuestalBC().GetList(codigoCentroCostos);
            ddlPartidaPresupuestal.DataTextField = "U_MSSP_NIV";
            ddlPartidaPresupuestal.DataValueField = "Code";
            ddlPartidaPresupuestal.DataBind();
        }
    }

    #endregion

    #region OnChange Selects

    protected void gvDocumentos_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        try
        {
            _TipoDocumentoWeb = (TipoDocumentoWeb)ViewState[ConstantHelper.Keys.TipoDocumentoWeb];
            _Modo = (Modo)ViewState[ConstantHelper.Keys.Modo];
            int idDetalleDocumento = Convert.ToInt32(e.CommandArgument.ToString());

            if (e.CommandName.Equals("Editar"))
            {
                lblIdDocumentoDetalle.Text = idDetalleDocumento.ToString();

                DocumentoWebRendicionBE objDocumentoBE = new DocumentoWebBC().GetDocumentoWebRendicion(idDetalleDocumento);
                txtSerie.Text = objDocumentoBE.SerieDoc;
                txtNumero.Text = objDocumentoBE.CorrelativoDoc.ToString();
                txtFecha.Text = objDocumentoBE.FechaDoc.ToString("dd/MM/yyyy");
                txtMontoTotal.Text = Convert.ToDouble(objDocumentoBE.MontoTotal).ToString("0.00");
                txtMontoDoc.Text = Convert.ToDouble(objDocumentoBE.MontoDoc).ToString("0.00");
                txtMontoAfecta.Text = Convert.ToDouble(objDocumentoBE.MontoAfecto).ToString("0.00");
                txtMontoNoAfecta.Text = Convert.ToDouble(objDocumentoBE.MontoNoAfecto).ToString("0.00");
                txtMontoIGV.Text = Convert.ToDouble(objDocumentoBE.MontoIGV).ToString("0.00");
                txtTasaCambio.Text = Convert.ToDouble(objDocumentoBE.TasaCambio).ToString("0.0000");

                if (objDocumentoBE.IdMonedaDoc == objDocumentoBE.IdMonedaOriginal)
                    txtTasaCambio.Enabled = false;
                else
                    txtTasaCambio.Enabled = false;
                ddlTipoDocumentoWeb.SelectedValue = objDocumentoBE.TipoDoc.ToString();

                txtProveedor.Text = objDocumentoBE.SAPProveedor;
                lblProveedor.Text = new ProveedorBC().GetCardNameProveedorSAP(objDocumentoBE.SAPProveedor);

                ddlIdMonedaDoc.SelectedValue = objDocumentoBE.IdMonedaDoc.ToString();
                ddlIdMonedaOriginal.SelectedValue = objDocumentoBE.IdMonedaOriginal.ToString();


                ListarPartidasPresupuestales(objDocumentoBE.IdCentroCostos1);
                if (objDocumentoBE.CodigoPartidaPresupuestal != null)
                    ddlPartidaPresupuestal.SelectedValue = objDocumentoBE.CodigoPartidaPresupuestal.ToString();

                Int32 IdEmpresa = new DocumentoWebBC().GetDocumentoWeb(objDocumentoBE.IdDocumentoWeb).IdEmpresa;

                ddlCentroCostos1.DataSource = new CentroCostosBC().ListarCentroCostos(1);
                ddlCentroCostos1.DataTextField = "Descripcion";
                ddlCentroCostos1.DataValueField = "IdCentroCostos";
                ddlCentroCostos1.DataBind();

                ddlCentroCostos2.DataSource = new CentroCostosBC().ListarCentroCostos(2);
                ddlCentroCostos2.DataTextField = "Descripcion";
                ddlCentroCostos2.DataValueField = "IdCentroCostos";
                ddlCentroCostos2.DataBind();

                ddlCentroCostos3.DataSource = new CentroCostosBC().ListarCentroCostos(3);
                ddlCentroCostos3.DataTextField = "Descripcion";
                ddlCentroCostos3.DataValueField = "IdCentroCostos";
                ddlCentroCostos3.DataBind();

                ddlCentroCostos4.DataSource = new CentroCostosBC().ListarCentroCostos(4);
                ddlCentroCostos4.DataTextField = "Descripcion";
                ddlCentroCostos4.DataValueField = "IdCentroCostos";
                ddlCentroCostos4.DataBind();

                ddlCentroCostos5.DataSource = new CentroCostosBC().ListarCentroCostos(5);
                ddlCentroCostos5.DataTextField = "Descripcion";
                ddlCentroCostos5.DataValueField = "IdCentroCostos";
                ddlCentroCostos5.DataBind();

                ddlConcepto.DataSource = new ConceptoBC().ListarConcepto();
                ddlConcepto.DataTextField = "Descripcion";
                ddlConcepto.DataValueField = "IdConcepto";
                ddlConcepto.DataBind();

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

                ddlConcepto.SelectedValue = objDocumentoBE.IdConcepto.ToString();

                bAgregar.Visible = false;
                bGuardar.Visible = true;
            }

            if (e.CommandName.Equals("Eliminar"))
            {
                new DocumentoWebBC().EliminarDocumentoDetalle(idDetalleDocumento);
                ListarRendicion();
            }
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(ex);
            Mensaje("Ocurrió un error: (NivelAprobacion): " + ex.Message);
        }
    }

    protected void gridView_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        gvDocumentos.PageIndex = e.NewPageIndex;
        ListarRendicion();
    }

    private void LlenarCabecera()
    {
        Int32 idDocumentoWeb = Convert.ToInt32(ViewState[ConstantHelper.Keys.IdDocumentoWeb].ToString());
        DocumentoWebBE objDocumentoBE = new DocumentoWebBC().GetDocumentoWeb(idDocumentoWeb);

        DocumentoWebRendicionBE objDocumentoDetalleBE = new DocumentoWebBC().GetDocumentoWebRendicion(null, idDocumentoWeb);
        string montoCCD = "0.00";
        if (objDocumentoDetalleBE != null)
            montoCCD = objDocumentoDetalleBE.MontoTotal.ToString();



        lblCabezera.Text = _TipoDocumentoWeb.GetName() + ": "
                            + objDocumentoBE.CodigoDocumento
                            + " - " + objDocumentoBE.Asunto
                            + " <br> Monto: " + new MonedaBC().ObtenerMoneda(objDocumentoBE.Moneda).Descripcion + " " 
                            +    objDocumentoBE.MontoActual.ToString("0.00")
                            + "/" + Convert.ToDouble(objDocumentoBE.MontoInicial).ToString("0.00");

        if (objDocumentoBE.Estado == "19")
            txtFechaContabilizacion.Text = txtFechaContabilizacion.Text = (objDocumentoBE.FechaContabilizacion).ToString("dd/MM/yyyy");
        else
            txtFechaContabilizacion.Text = txtFechaContabilizacion.Text = (DateTime.Today).ToString("dd/MM/yyyy");

    }

    private void LimpiarCampos()
    {
        txtSerie.Text = "";
        txtNumero.Text = "";
        txtFecha.Text = "";
        txtMontoTotal.Text = "";
        txtMontoDoc.Text = "";
        txtMontoAfecta.Text = "";
        txtMontoNoAfecta.Text = "";
        txtMontoIGV.Text = "";
        txtTasaCambio.Text = "";
        ddlTipoDocumentoWeb.SelectedValue = "0";
        ddlIdMonedaDoc.SelectedValue = "0";
        ddlCentroCostos1.SelectedValue = "0";
        ddlCentroCostos2.SelectedValue = "0";
        ddlCentroCostos3.SelectedValue = "0";
        ddlCentroCostos4.SelectedValue = "0";
        ddlCentroCostos5.SelectedValue = "0";
        ddlConcepto.SelectedValue = "0";
        ddlPartidaPresupuestal.SelectedValue = "0";
        ddlCuentaContableDevolucion.SelectedValue = "0";

        txtProveedor.Text = "";
        lblProveedor.Text = "sin validar";
    }

    protected void ddlIdMonedaDoc_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (ddlIdMonedaDoc.SelectedValue != "0")
        {
            if (ddlIdMonedaOriginal.SelectedValue == ddlIdMonedaDoc.SelectedValue)
            {
                txtTasaCambio.Text = "1.0000";
                txtTasaCambio.Enabled = false;
            }
            else
                txtTasaCambio.Enabled = true;
        }
        else
        {
            txtTasaCambio.Enabled = true;
        }
    }

    protected void chkRow_OnCheckedChanged(Object sender, EventArgs args)
    {
        if (gvDocumentos.Columns[0].Visible == true)
        {
            System.Web.UI.WebControls.CheckBox checkbox = (System.Web.UI.WebControls.CheckBox)sender;
            GridViewRow row = (GridViewRow)checkbox.NamingContainer;
            int Id = Convert.ToInt32(gvDocumentos.Rows[row.DataItemIndex].Cells[2].Text);

            DocumentoWebRendicionBE objDetalleDocumentoBE = new DocumentoWebBC().GetDocumentoWebRendicion(Id);

            if (checkbox.Checked == true)
                objDetalleDocumentoBE.Estado = "1";
            else
                objDetalleDocumentoBE.Estado = "2";

            new DocumentoWebBC().AddUpdateDocumentoWebRendicion(objDetalleDocumentoBE);
            LlenarCabecera();
        }
    }

    #endregion

    #region Submit Buttons

    protected void Aprobar_Click(object sender, EventArgs e)
    {
        try
        {
            _TipoDocumentoWeb = (TipoDocumentoWeb)ViewState[ConstantHelper.Keys.TipoDocumentoWeb];
            _Modo = (Modo)ViewState[ConstantHelper.Keys.Modo];

            _IdDocumentoWeb = Convert.ToInt32(ViewState[ConstantHelper.Keys.IdDocumentoWeb]);
            Int32 idUsuario = ((UsuarioBE)Session["Usuario"]).IdUsuario;

            //----------------------VALIDA-------------------------------
            if (String.IsNullOrEmpty(txtFechaContabilizacion.Text))
            {
                Mensaje("Debe ingresar un fecha de contabilizacion.");
                return;
            }
            //----------------------VALIDA-------------------------------

            bAprobar.Enabled = false;

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
            ExceptionHelper.LogException(ex);
            Mensaje("Ocurrió un error: " + ex.Message);
        }
        finally
        {
            bAprobar.Enabled = true;
            //Response.Redirect("~/ListadoDocumentos.aspx?TipoDocumentoWeb=" + (Int32)_TipoDocumentoWeb);

        }
    }

    protected void Agregar_Click(object sender, EventArgs e)
    {
        try
        {
            _TipoDocumentoWeb = (TipoDocumentoWeb)ViewState[ConstantHelper.Keys.TipoDocumentoWeb];
            _Modo = (Modo)ViewState[ConstantHelper.Keys.Modo];

            _IdDocumentoWeb = Convert.ToInt32(ViewState[ConstantHelper.Keys.IdDocumentoWeb]);
            GuardarDocumento(Modo.Crear);
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(ex);
            Mensaje("Ocurrió un erroR: " + ex.Message);
        }
        finally
        {
            bAgregar.Enabled = true;
        }
    }

    protected void Rechazar_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(txtComentario.Text))
                throw new Exception("Ingrese un comentario indicando la rendición que está rechazando");

            _TipoDocumentoWeb = (TipoDocumentoWeb)ViewState[ConstantHelper.Keys.TipoDocumentoWeb];
            _Modo = (Modo)ViewState[ConstantHelper.Keys.Modo];
            _IdDocumentoWeb = (Int32)base.ViewState[ConstantHelper.Keys.IdDocumentoWeb];
            Int32 idUsuario = ((UsuarioBE)Session["Usuario"]).IdUsuario;

            CambioEstadoBE cambioEstadoBE = new CambioEstadoBE()
            {
                IdDocumentoWeb = _IdDocumentoWeb,
                Comentario = txtComentario.Text,
                IdUsuario = idUsuario,
                TipoDocumentoOrigen = 2,
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
            //bRechazar.Enabled = true;
        }
    }

    //Agregar proveedor
    protected void Agregar2_Click(object sender, EventArgs e)
    {
        try
        {
            _TipoDocumentoWeb = (TipoDocumentoWeb)ViewState[ConstantHelper.Keys.TipoDocumentoWeb];
            _Modo = (Modo)ViewState[ConstantHelper.Keys.Modo];

            _IdDocumentoWeb = Convert.ToInt32(ViewState[ConstantHelper.Keys.IdDocumentoWeb]);
            Int32 idUsuario = ((UsuarioBE)Session["Usuario"]).IdUsuario;

            bAgregar2.Enabled = false;

            if (txtCardName.Text.Trim() != "" && txtDocumento.Text.Trim() != "")
            {
                ProveedorBC objProveedorBC = new ProveedorBC();
                ProveedorBE objProveedorBE = new ProveedorBE();
                objProveedorBE = objProveedorBC.ObtenerProveedor(0, 1, txtDocumento.Text);

                if (objProveedorBE == null)
                {
                    objProveedorBE = new ProveedorBE();
                    objProveedorBE.CardCode = "P" + txtDocumento.Text;
                    objProveedorBE.CardName = txtCardName.Text;
                    objProveedorBE.TipoDocumento = "6";
                    objProveedorBE.Documento = txtDocumento.Text;
                    objProveedorBE.IdProceso = _IdDocumentoWeb;
                    objProveedorBE.Proceso = 1;
                    objProveedorBE.Estado = 1;

                    if (Session["Usuario"] == null)
                    {
                        Response.Redirect("~/Login.aspx");
                    }
                    else
                    {
                        UsuarioBC objUsuarioBC = new UsuarioBC();
                        UsuarioBE objUsuarioBE = new UsuarioBE();
                        objUsuarioBE = (UsuarioBE)Session["Usuario"];
                        objUsuarioBE = objUsuarioBC.ObtenerUsuario(objUsuarioBE.IdUsuario, 0);

                        objProveedorBE.UserCreate = Convert.ToString(objUsuarioBE.IdUsuario);
                        objProveedorBE.CreateDate = DateTime.Now;
                        objProveedorBE.UserUpdate = Convert.ToString(objUsuarioBE.IdUsuario);
                        objProveedorBE.UpdateDate = DateTime.Now;
                    }
                    int Id;
                    Id = objProveedorBC.InsertarProveedor(objProveedorBE);
                    ListarProveedorCrear();
                    txtCardName.Text = ""; txtDocumento.Text = "";
                }
                else
                    Mensaje("El RUC ya existe.");
            }
            else
                Mensaje("Es necesario ingresar toda la informacion;");
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(ex);
            Mensaje("Ocurrió un error: " + ex.Message);
        }
        finally
        {
            bAgregar2.Enabled = true;
        }
    }

    protected void Agregar4_Click(object sender, EventArgs e)
    {
        try
        {
            _TipoDocumentoWeb = (TipoDocumentoWeb)ViewState[ConstantHelper.Keys.TipoDocumentoWeb];
            _Modo = (Modo)ViewState[ConstantHelper.Keys.Modo];

            Int32 idDocumento = Convert.ToInt32(ViewState[ConstantHelper.Keys.IdDocumentoWeb]);
            Int32 idUsuario = ((UsuarioBE)Session["Usuario"]).IdUsuario;

            string mensajeError = "";
            bool validacion = true;

            int iRows = GridView1.Rows.Count;
            if (iRows <= 0)
            {
                validacion = false;
                mensajeError = "No existe informacion que subir.";
            }

            List<string> sRUC = new List<string>();
            List<string> sProveedor = new List<string>();
            List<int> sIdProveedor = new List<int>();
            if (validacion)
            {
                double TasaCambio, No_Afecta, Afecta, IGV, Total_Documento, Total_Moneda_Origen;

                for (int i = 0; i < GridView1.Rows.Count; i++)
                {
                    TasaCambio = Convert.ToDouble(GridView1.Rows[i].Cells[8].Text);
                    No_Afecta = Convert.ToDouble(GridView1.Rows[i].Cells[9].Text);
                    Afecta = Convert.ToDouble(GridView1.Rows[i].Cells[10].Text);
                    IGV = Convert.ToDouble(GridView1.Rows[i].Cells[11].Text);
                    Total_Documento = Convert.ToDouble(GridView1.Rows[i].Cells[12].Text);
                    Total_Moneda_Origen = Convert.ToDouble(GridView1.Rows[i].Cells[13].Text);

                    sRUC.Add(GridView1.Rows[i].Cells[4].Text);
                    sProveedor.Add(GridView1.Rows[i].Cells[5].Text);
                    if (Math.Round(Total_Documento, 2) != Math.Round(IGV + Afecta + No_Afecta, 2))
                    {
                        validacion = false;
                        mensajeError = "La suma del IGV, Afecata y NoAfecta no es igual al Total.";
                    }
                }
            }


            ProveedorBC objProveedorBC = new ProveedorBC();
            ProveedorBE objProveedorBE = new ProveedorBE();
            if (validacion)
            {
                for (int i = 0; i < sRUC.Count; i++)
                {
                    objProveedorBE = objProveedorBC.ObtenerProveedor(0, 1, sRUC[i]);
                    if (objProveedorBE == null)
                    {
                        objProveedorBE = new ProveedorBE();
                        objProveedorBE.CardCode = "P" + sRUC[i];
                        objProveedorBE.CardName = sProveedor[i];
                        objProveedorBE.TipoDocumento = "6";
                        objProveedorBE.Documento = sRUC[i];
                        objProveedorBE.Proceso = 1;
                        objProveedorBE.IdProceso = idDocumento;
                        objProveedorBE.Estado = 1;

                        if (Session["Usuario"] == null)
                        {
                            Response.Redirect("~/Login.aspx");
                        }
                        else
                        {
                            UsuarioBC objUsuarioBC = new UsuarioBC();
                            UsuarioBE objUsuarioBE = new UsuarioBE();
                            objUsuarioBE = (UsuarioBE)Session["Usuario"];
                            objUsuarioBE = objUsuarioBC.ObtenerUsuario(objUsuarioBE.IdUsuario, 0);

                            objProveedorBE.UserCreate = Convert.ToString(objUsuarioBE.IdUsuario);
                            objProveedorBE.CreateDate = DateTime.Now;
                            objProveedorBE.UserUpdate = Convert.ToString(objUsuarioBE.IdUsuario);
                            objProveedorBE.UpdateDate = DateTime.Now;
                        }
                        int Id;
                        Id = objProveedorBC.InsertarProveedor(objProveedorBE);
                        sIdProveedor.Add(Id);
                    }
                    else
                    {
                        sIdProveedor.Add(objProveedorBE.IdProveedor);
                    }
                }
                ListarProveedorCrear();
            }

            if (validacion)
            {
                DocumentoWebRendicionBE objDocumentoBE;

                for (int i = 0; i < GridView1.Rows.Count; i++)
                {
                    objDocumentoBE = new DocumentoWebRendicionBE();
                    objDocumentoBE.IdDocumentoWeb = Convert.ToInt32(idDocumento);

                    //objDocumentoBE.IdProveedor = Convert.ToInt32(sIdProveedor[i]);
                    objDocumentoBE.SAPProveedor = GridView1.Rows[i].Cells[4].Text;
                    objDocumentoBE.IdConcepto = GridView1.Rows[i].Cells[6].Text;
                    objDocumentoBE.IdCentroCostos3 = ddlCentroCostos3.SelectedItem.Value;
                    objDocumentoBE.IdCentroCostos4 = ddlCentroCostos4.SelectedItem.Value;
                    objDocumentoBE.IdCentroCostos5 = ddlCentroCostos5.SelectedItem.Value;
                    objDocumentoBE.TipoDoc = GridView1.Rows[i].Cells[0].Text;
                    objDocumentoBE.SerieDoc = GridView1.Rows[i].Cells[1].Text;
                    objDocumentoBE.CorrelativoDoc = Convert.ToInt32(GridView1.Rows[i].Cells[2].Text);
                    objDocumentoBE.FechaDoc = Convert.ToDateTime(GridView1.Rows[i].Cells[3].Text);
                    objDocumentoBE.IdMonedaOriginal = Convert.ToInt32(ddlIdMonedaOriginal.SelectedItem.Value);
                    objDocumentoBE.IdMonedaDoc = Convert.ToInt32(GridView1.Rows[i].Cells[7].Text);
                    objDocumentoBE.TasaCambio = Convert.ToDecimal(GridView1.Rows[i].Cells[8].Text);
                    objDocumentoBE.MontoNoAfecto = Convert.ToDecimal(GridView1.Rows[i].Cells[9].Text);
                    objDocumentoBE.MontoAfecto = Convert.ToDecimal(GridView1.Rows[i].Cells[10].Text);
                    objDocumentoBE.MontoIGV = Convert.ToDecimal(GridView1.Rows[i].Cells[11].Text);
                    objDocumentoBE.MontoTotal = Convert.ToDecimal(GridView1.Rows[i].Cells[12].Text);
                    objDocumentoBE.MontoDoc = Convert.ToDecimal(GridView1.Rows[i].Cells[13].Text);
                    objDocumentoBE.Estado = "1";

                    if (Session["Usuario"] == null)
                    {
                        Response.Redirect("~/Login.aspx");
                    }
                    else
                    {
                        UsuarioBC objUsuarioBC = new UsuarioBC();
                        UsuarioBE objUsuarioBE = new UsuarioBE();
                        objUsuarioBE = (UsuarioBE)Session["Usuario"];
                        objUsuarioBE = objUsuarioBC.ObtenerUsuario(objUsuarioBE.IdUsuario, 0);

                        objDocumentoBE.UserCreate = objUsuarioBE.IdUsuario;
                        objDocumentoBE.CreateDate = DateTime.Now;
                        objDocumentoBE.UserUpdate = objUsuarioBE.IdUsuario;
                        objDocumentoBE.UpdateDate = DateTime.Now;
                    }
                    new DocumentoWebBC().AddUpdateDocumentoWebRendicion(objDocumentoBE);
                }

                ListarRendicion();
                LlenarCabecera();
                LimpiarCampos();

                blbResultadoMasivo.Visible = false;
                blbResultadoMasivo.Text = "";
                txtCopied.Visible = false;
                txtCopied.Text = "";
                GridView1.Visible = false;
                bPreliminar4.Visible = false;
                bAgregar4.Visible = false;
                bCancelar4.Visible = false;
                bMasivo.Visible = true;

                GridView1.DataSource = null;
                GridView1.DataBind();
            }
            else
                Mensaje(mensajeError);
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(ex);
            Mensaje("Ocurrió un error: " + ex.Message);
        }
        finally
        {
        }
    }

    protected void Guardar_Click(object sender, EventArgs e)
    {
        try
        {
            if (GuardarDocumento(Modo.Editar))
            {
                lblIdDocumentoDetalle.Text = "0";
                bAgregar.Visible = true;
                bGuardar.Visible = false;

                String estadoDocumento = new DocumentoWebBC().GetDocumentoWeb(Convert.ToInt32(ViewState[ConstantHelper.Keys.IdDocumentoWeb])).Estado;
                if (estadoDocumento == EstadoDocumento.Aprobado.IdToString())
                    bEnviar.Visible = true;
                else
                    bEnviar.Visible = false;
            }
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(ex);
            Mensaje("Ocurrió un error: " + ex.Message);
        }
        finally
        {
            bGuardar.Enabled = true;
        }
    }

    //Guardar proveedor
    protected void Guardar2_Click(object sender, EventArgs e)
    {
        try
        {
            _TipoDocumentoWeb = (TipoDocumentoWeb)ViewState[ConstantHelper.Keys.TipoDocumentoWeb];
            _Modo = (Modo)ViewState[ConstantHelper.Keys.Modo];

            Int32 idDocumento = Convert.ToInt32(ViewState[ConstantHelper.Keys.IdDocumentoWeb]);
            Int32 idUsuario = ((UsuarioBE)Session["Usuario"]).IdUsuario;

            bGuardar2.Enabled = false;

            if (txtCardName.Text.Trim() != "" && txtDocumento.Text.Trim() != "")
            {
                ProveedorBC objProveedorBC = new ProveedorBC();
                ProveedorBE objProveedorBE = new ProveedorBE();

                objProveedorBE.IdProveedor = Convert.ToInt32(lblIdProveedor.Text);
                objProveedorBE.CardCode = "P" + txtDocumento.Text;
                objProveedorBE.CardName = txtCardName.Text;
                objProveedorBE.TipoDocumento = "6";
                objProveedorBE.Documento = txtDocumento.Text;


                objProveedorBE.Proceso = 1;
                objProveedorBE.IdProceso = idDocumento;
                objProveedorBE.Estado = 1;

                if (Session["Usuario"] == null)
                {
                    Response.Redirect("~/Login.aspx");
                }
                else
                {
                    UsuarioBC objUsuarioBC = new UsuarioBC();
                    UsuarioBE objUsuarioBE = new UsuarioBE();
                    objUsuarioBE = (UsuarioBE)Session["Usuario"];
                    objUsuarioBE = objUsuarioBC.ObtenerUsuario(objUsuarioBE.IdUsuario, 0);

                    objProveedorBE.UserCreate = Convert.ToString(objUsuarioBE.IdUsuario);
                    objProveedorBE.CreateDate = DateTime.Now;
                    objProveedorBE.UserUpdate = Convert.ToString(objUsuarioBE.IdUsuario);
                    objProveedorBE.UpdateDate = DateTime.Now;
                }
                objProveedorBC.ModificarProveedor(objProveedorBE);
                ListarProveedorCrear();
                txtCardName.Text = ""; txtDocumento.Text = "";

                bGuardar2.Visible = false;
                bAgregar2.Visible = true;
            }
            else
                Mensaje("Es necesario ingresar toda la informacion;");
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(ex);
            Mensaje("Ocurrió un error: " + ex.Message);
        }
        finally
        {
            bGuardar2.Enabled = true;
        }
    }


    protected void Cancelar_Click(object sender, EventArgs e)
    {
        _TipoDocumentoWeb = (TipoDocumentoWeb)ViewState[ConstantHelper.Keys.TipoDocumentoWeb];
        _Modo = (Modo)ViewState[ConstantHelper.Keys.Modo];

        Int32 idDocumento = Convert.ToInt32(ViewState[ConstantHelper.Keys.IdDocumentoWeb]);
        Int32 idUsuario = ((UsuarioBE)Session["Usuario"]).IdUsuario;
        Response.Redirect("~/ListadoDocumentos.aspx?TipoDocumentoWeb=" + (Int32)_TipoDocumentoWeb);


    }
    protected void Cancelar4_Click(object sender, EventArgs e)
    {
        blbResultadoMasivo.Visible = false;
        blbResultadoMasivo.Text = "";
        txtCopied.Visible = false;
        txtCopied.Text = "";
        GridView1.Visible = false;
        bPreliminar4.Visible = false;
        bAgregar4.Visible = false;
        bCancelar4.Visible = false;
        bMasivo.Visible = true;

        GridView1.DataSource = null;
        GridView1.DataBind();
    }

    protected void Enviar_Click(object sender, EventArgs e)
    {
        try
        {
            _TipoDocumentoWeb = (TipoDocumentoWeb)ViewState[ConstantHelper.Keys.TipoDocumentoWeb];
            _Modo = (Modo)ViewState[ConstantHelper.Keys.Modo];

            _IdDocumentoWeb = Convert.ToInt32(ViewState[ConstantHelper.Keys.IdDocumentoWeb]);
            Int32 idUsuario = ((UsuarioBE)Session["Usuario"]).IdUsuario;

            bEnviar.Enabled = false;

            if (gvDocumentos.Rows.Count > 0)
            {
                new DocumentoWebBC().EnviarRendicion(_IdDocumentoWeb);
                Response.Redirect("~/ListadoDocumentos.aspx?TipoDocumentoWeb=" + (Int32)_TipoDocumentoWeb);
            }
            else
                Mensaje("Aun no se ah rendido ningun documento.");
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(ex);
            Mensaje("Ocurrió un error: " + ex.Message);
        }
        finally
        {
            bEnviar.Enabled = true;
        }
    }

    //Arturo Rodriguez Liquidar
    protected void bLiquidar_Click(object sender, EventArgs e)
    {
        try
        {
            _TipoDocumentoWeb = (TipoDocumentoWeb)ViewState[ConstantHelper.Keys.TipoDocumentoWeb];
            _Modo = (Modo)ViewState[ConstantHelper.Keys.Modo];

            _IdDocumentoWeb = Convert.ToInt32(ViewState[ConstantHelper.Keys.IdDocumentoWeb]);
            Int32 idUsuario = ((UsuarioBE)Session["Usuario"]).IdUsuario;

            //----------------------VALIDA-------------------------------
            if (String.IsNullOrEmpty(txtFechaContabilizacion.Text))
            {
                Mensaje("Debe ingresar un fecha de contabilizacion.");
                return;
            }
            //----------------------VALIDA-------------------------------

            bLiquidar.Enabled = false;

            CambioEstadoBE cambioEstadoBE = new CambioEstadoBE()
            {
                IdDocumentoWeb = _IdDocumentoWeb,
                Comentario = txtComentario.Text,
                IdUsuario = idUsuario
            };

            new DocumentoWebBC().AprobarYLiquidarDocumento(cambioEstadoBE);
            Response.Redirect("~/ListadoDocumentos.aspx?TipoDocumentoWeb=" + (Int32)_TipoDocumentoWeb);
        }
        catch (Exception ex)
        {
            Mensaje("Ocurrió un error: " + ex.Message);
            ExceptionHelper.LogMessage(ex.Message);
            ExceptionHelper.LogException(ex);
        }
        finally
        {
            bLiquidar.Enabled = true;
        }

    }
    // Arturo Rodriguez Liquidar

    public Boolean GuardarDocumento(Modo modoGuardado)
    {
        _IdDocumentoWeb = Convert.ToInt32(ViewState[ConstantHelper.Keys.IdDocumentoWeb]);
        String errorMessage;

        CamposSonValidos(out errorMessage);
        if (!String.IsNullOrEmpty(errorMessage))
        {
            Mensaje(errorMessage);
            return false;
        }

        if (modoGuardado == Modo.Crear)
            bAgregar.Enabled = false;
        else if (modoGuardado == Modo.Editar)
            bGuardar.Enabled = false;

        _TipoDocumentoWeb = (TipoDocumentoWeb)ViewState[ConstantHelper.Keys.TipoDocumentoWeb];
        _Modo = (Modo)ViewState[ConstantHelper.Keys.Modo];

        Int32 idUsuario = ((UsuarioBE)Session["Usuario"]).IdUsuario;

        DocumentoWebRendicionBE documentDetailBE = new DocumentoWebRendicionBE();
        documentDetailBE.IdDocumentoWeb = _IdDocumentoWeb;
        documentDetailBE.IdDocumentoWebRendicion = Convert.ToInt32(lblIdDocumentoDetalle.Text);
        documentDetailBE.TipoDoc = ddlTipoDocumentoWeb.SelectedItem.Value;
        documentDetailBE.SerieDoc = txtSerie.Text;
        documentDetailBE.CorrelativoDoc = Convert.ToInt32(txtNumero.Text);
        documentDetailBE.FechaDoc = DateTime.ParseExact(txtFecha.Text, "dd/MM/yyyy", CultureInfo.InvariantCulture);

        var cardCodeSAP = new ProveedorBC().GetCardCodeProveedorSAP(txtProveedor.Text);
        if (string.IsNullOrEmpty(cardCodeSAP))
        {
            cardCodeSAP = new ProveedorBC().ObtenerProveedorPorDocumento(txtProveedor.Text).CardCode;
            if (string.IsNullOrEmpty(cardCodeSAP))
                throw new Exception("No se encontró el proveedor en la base de datos interna ni en SAP.");
        }

        documentDetailBE.SAPProveedor = cardCodeSAP;
        documentDetailBE.IdConcepto = ddlConcepto.SelectedItem.Value;
        if (ddlPartidaPresupuestal.SelectedItem != null)
            documentDetailBE.CodigoPartidaPresupuestal = ddlPartidaPresupuestal.SelectedItem.Value;
        documentDetailBE.IdCentroCostos1 = ddlCentroCostos1.SelectedItem.Value;
        documentDetailBE.IdCentroCostos2 = ddlCentroCostos2.SelectedItem.Value;
        documentDetailBE.IdCentroCostos3 = ddlCentroCostos3.SelectedItem.Value;
        documentDetailBE.IdCentroCostos4 = ddlCentroCostos4.SelectedItem.Value;
        documentDetailBE.IdCentroCostos5 = ddlCentroCostos5.SelectedItem.Value;
        documentDetailBE.IdMonedaOriginal = Convert.ToInt32(ddlIdMonedaOriginal.SelectedItem.Value);
        documentDetailBE.IdMonedaDoc = Convert.ToInt32(ddlIdMonedaDoc.SelectedItem.Value);
        documentDetailBE.MontoAfecto = Convert.ToDecimal(txtMontoAfecta.Text);
        documentDetailBE.MontoNoAfecto = Convert.ToDecimal(txtMontoNoAfecta.Text);
        documentDetailBE.MontoIGV = Convert.ToDecimal(txtMontoIGV.Text);
        documentDetailBE.MontoDoc = Convert.ToDecimal(txtMontoDoc.Text);
        documentDetailBE.MontoTotal = Convert.ToDecimal(txtMontoTotal.Text);
        documentDetailBE.CodigoCuentaContableDevolucion = ddlCuentaContableDevolucion.SelectedItem.Value;
        documentDetailBE.UserUpdate = idUsuario;

        if (ddlIdMonedaOriginal.SelectedValue == ddlIdMonedaDoc.SelectedValue)
            documentDetailBE.TasaCambio = 1;
        else
            documentDetailBE.TasaCambio = Convert.ToDecimal(txtTasaCambio.Text);

        if (Session["Usuario"] == null)
            Response.Redirect("~/Login.aspx");
        else
        {
            documentDetailBE.UserCreate = idUsuario;
            documentDetailBE.UserUpdate = idUsuario;
            documentDetailBE.CreateDate = DateTime.Now;
            documentDetailBE.UpdateDate = DateTime.Now;
        }

        new DocumentoWebBC().AddUpdateDocumentoWebRendicion(documentDetailBE);
        ListarRendicion();
        LlenarCabecera();
        LimpiarCampos();
        return true;
    }

    private void Mensaje(String mensaje)
    {
        var message = mensaje.Replace("'", "");
        ScriptManager.RegisterStartupScript(Page, this.GetType(), "MessageBox", "alert('" + message + "')", true);
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

    public Boolean CamposSonValidos(out String errorMessage)
    {
        /*---------------------------------------VALIDA CAMPOS REQUERIDOS------------------------------------------------*/
        errorMessage = String.Empty;
        var indexNoValidos = new[] { "0", "-1" };
        if (indexNoValidos.Contains(ddlTipoDocumentoWeb.SelectedValue))
            errorMessage = "Debe ingresar el Tipo.";
        else if (String.IsNullOrWhiteSpace(txtSerie.Text))
            errorMessage = "Debe ingresar la serie.";
        else if (String.IsNullOrWhiteSpace(txtNumero.Text))
            errorMessage = "Debe ingresar el numero.";
        else if (String.IsNullOrWhiteSpace(txtFecha.Text))
            errorMessage = "Debe ingresar la fecha.";
        else if ((TipoDocumentoSunat)Convert.ToInt32(ddlTipoDocumentoWeb.SelectedValue) != TipoDocumentoSunat.Devolucion
              && String.IsNullOrWhiteSpace(txtProveedor.Text))
            errorMessage = "Debe ingresar el RUC.";
        else if ((TipoDocumentoSunat)Convert.ToInt32(ddlTipoDocumentoWeb.SelectedValue) != TipoDocumentoSunat.Devolucion
                && !new ValidationHelper().ProveedorExisteEnBDIOSAP(txtProveedor.Text))
            errorMessage = "El proveedor no existe";
        else if ((TipoDocumentoSunat)Convert.ToInt32(ddlTipoDocumentoWeb.SelectedValue) != TipoDocumentoSunat.Devolucion
                && indexNoValidos.Contains(ddlConcepto.SelectedValue))
            errorMessage = "Debe ingresar concepto.";
        else if (new DocumentoBC().ObtenerDocumento(Convert.ToInt32(ddlTipoDocumentoWeb.SelectedValue)).CodigoSunat == TipoDocumentoSunat.Devolucion.GetCodigoSunat()
                && indexNoValidos.Contains(ddlCuentaContableDevolucion.SelectedValue))
            errorMessage = "Debe ingresar la cuenta contable.";
        else if (indexNoValidos.Contains(ddlCentroCostos1.SelectedValue))
            errorMessage = "Debe ingresar el centro de costo nivel 1";
        //else if (indexNoValidos.Contains(ddlPartidaPresupuestal.SelectedIndex)) //TODO: EN PROD
        //    errorMessage = "Debe ingresar la partida presupuestal.";
        else if (indexNoValidos.Contains(ddlIdMonedaDoc.SelectedValue))
            errorMessage = "Debe ingresar la  moneda del documento.";
        else if (String.IsNullOrWhiteSpace(txtMontoAfecta.Text) && String.IsNullOrWhiteSpace(txtMontoNoAfecta.Text))
            errorMessage = "Debe ingresar los importes";
        else if (String.IsNullOrWhiteSpace(txtMontoTotal.Text))
            errorMessage = "Aún no se han validado los importes.";
        else if (!txtMontoDoc.Text.IsNumeric()
                || !txtTasaCambio.Text.IsNumeric()
                || !txtMontoAfecta.Text.IsNumeric()
                || !txtMontoNoAfecta.Text.IsNumeric())
            errorMessage = "Los importes ingresados no son válidos";
        else if (!ValidarImporte())
            errorMessage = "No ha ingresado los importes correctamente";
        else if (Math.Round(Convert.ToDouble(txtMontoDoc.Text), 2)
                != Math.Round(Convert.ToDouble(txtMontoIGV.Text)
                + Convert.ToDouble(txtMontoAfecta.Text)
                + Convert.ToDouble(txtMontoNoAfecta.Text), 2))
            errorMessage = "La suma del IGV, Afecta y NoAfecta no es igual al Total.";
        else if (Convert.ToDouble(txtMontoDoc.Text) > new ValidationHelper().ObtenerMontoMaximoDeDocumento(_TipoDocumentoWeb, ddlIdMonedaDoc.SelectedItem.Text))
            errorMessage = "El monto total del documento excede al monto máximo permitido: " + new ValidationHelper().ObtenerMontoMaximoDeDocumento(_TipoDocumentoWeb, ddlIdMonedaDoc.SelectedItem.Text);

        if (!String.IsNullOrEmpty(errorMessage))
            return false;
        else
            return true;
        /*-------------------------------------FIN VALIDA CAMPOS REQUERIDOS----------------------------------------------*/

    }

    protected void Validar_Click(object sender, EventArgs e)
    {
        try
        {
            lblProveedor.Text = "Validando...";
            var cardName = new ProveedorBC().GetCardNameProveedorSAP(txtProveedor.Text);
            if (string.IsNullOrEmpty(cardName))
            {
                var proveedorBDSICER = new ProveedorBC().ObtenerProveedor(0, 1, txtProveedor.Text);
                if (proveedorBDSICER != null)
                {
                    if (!string.IsNullOrEmpty(proveedorBDSICER.CardName))
                    {
                        cardName = proveedorBDSICER.CardName;
                    }
                }
            }
            if (string.IsNullOrEmpty(cardName))
            {
                lblProveedor.Text = string.Empty;
                lblProveedor.Text = "Proveedor no existe en SAP.";

            }
            else
            {
                lblProveedor.Text = string.Empty;
                lblProveedor.Text = cardName;
            }
        }
        catch (Exception ex)
        {
            Mensaje(ex.Message);
        }
    }

    protected void ValidarImporte_Click(object sender, EventArgs e)
    {
        bool validacion = true;
        decimal n;
        bool isNumeric1, isNumeric2, isNumeric3;
        if (txtTasaCambio.Text.Trim() != "")
        {
            isNumeric1 = decimal.TryParse(txtTasaCambio.Text, out n);
            if (isNumeric1 == false) validacion = false;
        }
        else txtTasaCambio.Text = "1.00";

        if (txtMontoAfecta.Text.Trim() != "")
        {
            isNumeric2 = decimal.TryParse(txtMontoAfecta.Text, out n);
            if (isNumeric2 == false) validacion = false;
        }
        else txtMontoAfecta.Text = "0.00";

        if (txtMontoNoAfecta.Text.Trim() != "")
        {
            isNumeric3 = decimal.TryParse(txtMontoNoAfecta.Text, out n);
            if (isNumeric3 == false) validacion = false;
        }
        else txtMontoNoAfecta.Text = "0.00";

        if (validacion)
        {
            double MontoAfecta = Math.Round(Convert.ToDouble(txtMontoAfecta.Text), 2);
            double MontoNoAfecta = Math.Round(Convert.ToDouble(txtMontoNoAfecta.Text), 2);
            double MontoIGV = Math.Round(MontoAfecta * 0.18, 2);
            double MontoDoc = Math.Round(MontoAfecta + MontoNoAfecta + MontoIGV, 2);
            double MontoTotal = Math.Round(MontoDoc * Convert.ToDouble(txtTasaCambio.Text), 2);

            txtMontoIGV.Text = Math.Round(MontoIGV, 2).ToString("0.00");
            txtMontoDoc.Text = Math.Round(MontoDoc, 2).ToString("0.00");
            txtMontoTotal.Text = Math.Round(MontoTotal, 2).ToString("0.00");
            txtMontoAfecta.Text = Math.Round(Convert.ToDouble(txtMontoAfecta.Text), 2).ToString("0.00");
            txtMontoNoAfecta.Text = Math.Round(Convert.ToDouble(txtMontoNoAfecta.Text), 2).ToString("0.00");

            if (MontoDoc == 0) Mensaje("Monto Total debe ser mayor a 0.");
        }
        else
            Mensaje("Usted a ingresado los importes erroneamente.");
    }

    private bool ValidarImporte()
    {
        bool validacion = true;
        decimal n;
        bool isNumeric1, isNumeric2, isNumeric3;
        if (txtTasaCambio.Text.Trim() != "")
        {
            isNumeric1 = decimal.TryParse(txtTasaCambio.Text, out n);
            if (isNumeric1 == false) validacion = false;
        }
        else txtTasaCambio.Text = "1.0000";

        if (txtMontoAfecta.Text.Trim() != "")
        {
            isNumeric2 = decimal.TryParse(txtMontoAfecta.Text, out n);
            if (isNumeric2 == false) validacion = false;
        }
        else txtMontoAfecta.Text = "0.00";

        if (txtMontoNoAfecta.Text.Trim() != "")
        {
            isNumeric3 = decimal.TryParse(txtMontoNoAfecta.Text, out n);
            if (isNumeric3 == false) validacion = false;
        }
        else txtMontoNoAfecta.Text = "0.00";

        if (validacion)
        {
            double MontoAfecta = Math.Round(Convert.ToDouble(txtMontoAfecta.Text), 2);
            double MontoNoAfecta = Math.Round(Convert.ToDouble(txtMontoNoAfecta.Text), 2);
            double MontoIGV = Math.Round(MontoAfecta * 0.18, 2);
            double MontoDoc = Math.Round(MontoAfecta + MontoNoAfecta + MontoIGV, 2);
            double MontoTotal = Math.Round(MontoDoc * Convert.ToDouble(txtTasaCambio.Text), 2);

            txtMontoIGV.Text = Math.Round(MontoIGV, 2).ToString("0.00");
            txtMontoDoc.Text = Math.Round(MontoDoc, 2).ToString("0.00");
            txtMontoTotal.Text = Math.Round(MontoTotal, 2).ToString("0.00");
            txtMontoAfecta.Text = Math.Round(Convert.ToDouble(txtMontoAfecta.Text), 2).ToString("0.00");
            txtMontoNoAfecta.Text = Math.Round(Convert.ToDouble(txtMontoNoAfecta.Text), 2).ToString("0.00");

            if (MontoDoc == 0) validacion = false;
        }
        else validacion = false;

        return validacion;
    }

    #endregion

    protected void gvProveedor_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        int IdProveedor;

        try
        {
            IdProveedor = Convert.ToInt32(e.CommandArgument.ToString());

            if (e.CommandName.Equals("Editar"))
            {
                lblIdProveedor.Text = IdProveedor.ToString();

                ProveedorBC objProveedorBC = new ProveedorBC();
                ProveedorBE objProveedorBE = new ProveedorBE();
                objProveedorBE = objProveedorBC.ObtenerProveedor(IdProveedor, 0, "");
                txtCardName.Text = objProveedorBE.CardName;
                txtDocumento.Text = objProveedorBE.Documento;

                bAgregar2.Visible = false;
                bGuardar2.Visible = true;
            }
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(ex);
            Mensaje("Ocurrió un error: (NivelAprobacion): " + ex.Message);
        }
    }

    protected void gridViewP_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        gvProveedor.PageIndex = e.NewPageIndex;
        ListarProveedorCrear();
    }

    protected void ddlConcepto_SelectedIndexChanged(object sender, EventArgs e)
    {

    }

    protected void ddlTipoDocumentoWeb_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (new DocumentoBC().ObtenerDocumento(Convert.ToInt32(ddlTipoDocumentoWeb.SelectedValue)).CodigoSunat == TipoDocumentoSunat.Devolucion.GetCodigoSunat())
        {
            ddlCuentaContableDevolucion.Enabled = true;

            _IdDocumentoWeb = Convert.ToInt32(ViewState[ConstantHelper.Keys.IdDocumentoWeb]);
            Int32 idUsuarioDueñoDocumento = new DocumentoWebBC().GetDocumentoWeb(_IdDocumentoWeb).IdUsuarioSolicitante;
            UsuarioBE usuario = new UsuarioBC().ObtenerUsuario(idUsuarioDueñoDocumento, 0);

            if (usuario != null)
            {
                lblProveedor.Text = usuario.CardName;
                txtProveedor.Enabled = false;
                ddlConcepto.Enabled = false;
                btnValidar.Visible = false;
                txtMontoAfecta.Text = "0";
                txtMontoAfecta.Enabled = false;
            }
            else
                throw new Exception("No se encontraron datos de usuario.");
        }
        else
        {
            lblProveedor.Text = "";
            ddlConcepto.Enabled = true;
            txtProveedor.Enabled = true;
            lblProveedor.Text = String.Empty;
            btnValidar.Visible = true;
            txtMontoAfecta.Enabled = true;

            ddlCuentaContableDevolucion.SelectedValue = "0";
            ddlCuentaContableDevolucion.Enabled = false;
        }
    }

    protected void ddlCentroCostos1_SelectedIndexChanged(object sender, EventArgs e)
    {
        ListarPartidasPresupuestales(ddlCentroCostos1.SelectedItem.Value);
    }
}

