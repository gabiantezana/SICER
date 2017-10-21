﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSS.TAWA.BE
{
    public class DocumentDetailBE
    {
        int _IdDocumentoDetalle;
        int _IdDocumento;
        int _IdProveedor;
        String _IdConcepto;
        String _IdCentroCostos1;
        String _IdCentroCostos2;
        String _IdCentroCostos3;
        String _IdCentroCostos4;
        String _IdCentroCostos5;
        int _Rendicion;
        /// <summary>
        /// TODO: Partida presupuestal
        /// </summary>
        String _PartidaPresupuestal;
        String _TipoDoc;
        String _SerieDoc;
        String _CorrelativoDoc;
        DateTime _FechaDoc;
        int _IdMonedaDoc;
        String _MontoDoc;
        String _TasaCambio;
        int _IdMonedaOriginal;
        String _MontoNoAfecto;
        String _MontoAfecto;
        String _MontoIGV;
        String _MontoTotal;
        String _Estado;
        String _UserCreate;
        DateTime _CreateDate;
        String _UserUpdate;
        DateTime _UpdateDate;

        public int IdDocumentoDetalle
        {
            get { return _IdDocumentoDetalle; }
            set { _IdDocumentoDetalle = value; }
        }
        public int IdDocumento
        {
            get { return _IdDocumento; }
            set { _IdDocumento = value; }
        }
        public int IdProveedor
        {
            get { return _IdProveedor; }
            set { _IdProveedor = value; }
        }
        public String IdConcepto
        {
            get { return _IdConcepto; }
            set { _IdConcepto = value; }
        }
        public String IdCentroCostos1
        {
            get { return _IdCentroCostos1; }
            set { _IdCentroCostos1 = value; }
        }
        public String IdCentroCostos2
        {
            get { return _IdCentroCostos2; }
            set { _IdCentroCostos2 = value; }
        }
        public String IdCentroCostos3
        {
            get { return _IdCentroCostos3; }
            set { _IdCentroCostos3 = value; }
        }
        public String IdCentroCostos4
        {
            get { return _IdCentroCostos4; }
            set { _IdCentroCostos4 = value; }
        }
        public String IdCentroCostos5
        {
            get { return _IdCentroCostos5; }
            set { _IdCentroCostos5 = value; }
        }
        public int Rendicion
        {
            get { return _Rendicion; }
            set { _Rendicion = value; }
        }
        public String TipoDoc
        {
            get { return _TipoDoc; }
            set { _TipoDoc = value; }
        }
        public String SerieDoc
        {
            get { return _SerieDoc; }
            set { _SerieDoc = value; }
        }
        public String CorrelativoDoc
        {
            get { return _CorrelativoDoc; }
            set { _CorrelativoDoc = value; }
        }
        public DateTime FechaDoc
        {
            get { return _FechaDoc; }
            set { _FechaDoc = value; }
        }
        public int IdMonedaDoc
        {
            get { return _IdMonedaDoc; }
            set { _IdMonedaDoc = value; }
        }
        public String MontoDoc
        {
            get { return _MontoDoc; }
            set { _MontoDoc = value; }
        }
        public String TasaCambio
        {
            get { return _TasaCambio; }
            set { _TasaCambio = value; }
        }
        public int IdMonedaOriginal
        {
            get { return _IdMonedaOriginal; }
            set { _IdMonedaOriginal = value; }
        }
        public String MontoNoAfecto
        {
            get { return _MontoNoAfecto; }
            set { _MontoNoAfecto = value; }
        }
        public String MontoAfecto
        {
            get { return _MontoAfecto; }
            set { _MontoAfecto = value; }
        }
        public String MontoIGV
        {
            get { return _MontoIGV; }
            set { _MontoIGV = value; }
        }
        public String MontoTotal
        {
            get { return _MontoTotal; }
            set { _MontoTotal = value; }
        }
        public String Estado
        {
            get { return _Estado; }
            set { _Estado = value; }
        }
        public String UserCreate
        {
            get { return _UserCreate; }
            set { _UserCreate = value; }
        }
        public DateTime CreateDate
        {
            get { return _CreateDate; }
            set { _CreateDate = value; }
        }
        public String UserUpdate
        {
            get { return _UserUpdate; }
            set { _UserUpdate = value; }
        }
        public DateTime UpdateDate
        {
            get { return _UpdateDate; }
            set { _UpdateDate = value; }
        }
        public String PartidaPresupuestal
        {
            get { return _PartidaPresupuestal; }
            set { _PartidaPresupuestal = value; }
        }

    }
}