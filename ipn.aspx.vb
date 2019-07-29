#Region "References"
Imports System.Security.Cryptography
#End Region

''' <summary>
''' 2Checkout Instant Payment Notification
''' Copyright (c) 2019 Smart PC Utilities, Ltd.
''' </summary>
Public Class IPN

    Inherits Page

#Region "Declarations"

    Private _orderInfo As OrderInfo

    Const MY_SECRET_KEY As String = "2Checkout Secret Key"

#End Region

#Region "Page Events"

    Private Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

        If Request.Form.AllKeys.Count = 0 Then
            SendResponse(400, "Null HTTP POST")
            Return
        End If

        _orderInfo = New OrderInfo(Request.Form)

        If _orderInfo.HashDataReceived = _orderInfo.IpnSignature Then 'Hash signature validation OK

            Response.Write("<EPAYMENT>" & _orderInfo.DateReturned & "|" & _orderInfo.HashDataReturned & "</EPAYMENT>")

            'Execute your code here

        Else
            SendResponse(400, "Invalid Signature")
        End If

    End Sub

#End Region

#Region "Helper Methods"

    Private Shared Sub SendResponse(Optional responseCode As Integer = 200, Optional responseContent As String = "", Optional responseContentType As String = "text/plain")

        With HttpContext.Current

            .Response.StatusCode = responseCode
            .Response.Clear()

            If Not String.IsNullOrWhiteSpace(responseContent) Then
                .Response.ContentEncoding = Encoding.UTF8
                .Response.ContentType = responseContentType
                .Response.Output.Write(responseContent)
            End If

            .Response.Flush()
            .Response.SuppressContent = True
            .ApplicationInstance.CompleteRequest()

        End With

    End Sub

    Friend Shared Function MyHmacMd5(msg As String, key As String) As String

        Dim encoding = New ASCIIEncoding()

        Dim msgByte As Byte() = encoding.GetBytes(msg)
        Dim keyByte As Byte() = encoding.GetBytes(key)

        Dim hmacMd5 = New HMACMD5(keyByte)
        Dim msgHash As Byte() = hmacMd5.ComputeHash(msgByte)

        Dim strBldHmac As New StringBuilder

        For i = 0 To msgHash.Length - 1
            strBldHmac.Append(msgHash(i).ToString("x2"))
        Next

        Return (strBldHmac.ToString)

    End Function

    Private Shared Function StripSlashes(input As String) As String

        If Len(input) > 0 Then

            input = Replace(input, "\\", "\")
            input = Replace(input, "\'", "'")

        End If

        Return input

    End Function

#End Region

#Region "Structures"

    Private Structure OrderInfo

#Region "Members"

        ''' <summary>
        '''     Whether the order is a test order or not
        ''' </summary>
        Public ReadOnly IsTestOrder As Boolean

        ''' <summary>
        '''     2Checkout order reference number
        ''' </summary>
        Public ReadOnly RefNumber As Integer

        ''' <summary>
        '''     External order reference number
        ''' </summary>
        Public ReadOnly RefNumberExt As String

        ''' <summary>
        '''     Payment interface language
        ''' </summary>
        Public ReadOnly LangCode As String

        ''' <summary>
        '''     Customer email address
        ''' </summary>
        Public ReadOnly CustomerEmail As String

        ''' <summary>
        '''     Partner code (If empty = e-commerce order)
        ''' </summary>
        Public ReadOnly PartnerCode As String

        ''' <summary>
        '''     Order status (PENDING, PAYMENT_AUTHORIZED, SUSPECT, INVALID, COMPLETE, REFUND, REVERSED, PURCHASE_PENDING,
        '''     PAYMENT_RECEIVED, CANCELED, PENDING_APPROVAL)
        ''' </summary>
        Public ReadOnly OrderStatus As String

        ''' <summary>
        '''     Ordered products info
        ''' </summary>
        Public ReadOnly LstProducts As List(Of ProductInfo)

        ''' <summary>
        '''     Order payment method
        ''' </summary>
        Public ReadOnly PaymentMethod As String

        ''' <summary>
        '''     The date-time stamp when shopper place order
        ''' </summary>
        Public ReadOnly OrderPlacementDate As Date

        ''' <summary>
        '''     Currency used in order
        ''' </summary>
        Public ReadOnly OrderCurrency As String

        ''' <summary>
        '''     IPN notification date
        ''' </summary>
        Public ReadOnly IpnDate As String

        ''' <summary>
        '''     Md5 HMAC signature provided by 2Checkout
        ''' </summary>
        Public ReadOnly IpnSignature As String

        ''' <summary>
        '''     Md5 HMAC hash calculated using received data
        ''' </summary>
        Public ReadOnly HashDataReceived As String

        ''' <summary>
        '''     Md5 HMAC hash calculated using returned data
        ''' </summary>
        Public ReadOnly HashDataReturned As String

        ''' <summary>
        '''     Date returned to 2Checkout
        ''' </summary>
        Public ReadOnly DateReturned As String

#End Region

#Region "Methods"

        Public Sub New(htmlPost As NameValueCollection)

            OrderStatus = htmlPost("ORDERSTATUS")

            If htmlPost("TEST_ORDER").Equals("1", StringComparison.InvariantCultureIgnoreCase) Then
                IsTestOrder = True
            Else
                IsTestOrder = False
            End If

            If Not Date.TryParse(htmlPost("SALEDATE"), OrderPlacementDate) Then
                OrderPlacementDate = Date.UtcNow
            End If

            If Not Integer.TryParse(htmlPost("REFNO"), RefNumber) Then
                RefNumber = 0
            End If

            If htmlPost("REFNOEXT") IsNot Nothing Then
                RefNumberExt = htmlPost("REFNOEXT")
            Else
                RefNumberExt = String.Empty
            End If

            LstProducts = New List(Of ProductInfo)

            Dim lstProductsPid As String() = htmlPost.GetValues("IPN_PID[]")
            Dim lstProductsName As String() = htmlPost.GetValues("IPN_PNAME[]")
            Dim lstProductsCode As String() = htmlPost.GetValues("IPN_PCODE[]")
            Dim lstProductsQuantity As String() = htmlPost.GetValues("IPN_QTY[]")

            For I = 0 To lstProductsPid.Count - 1

                Dim product As New ProductInfo With {.Id = CInt(lstProductsPid(I)), .Code = lstProductsCode(I), .Name = lstProductsName(I), .Quantity = CInt(lstProductsQuantity(I))}
                LstProducts.Add(product)

            Next

            LangCode = htmlPost("LANGUAGE")
            CustomerEmail = htmlPost("CUSTOMEREMAIL")

            If htmlPost("IPN_PARTNER_CODE") IsNot Nothing Then
                PartnerCode = htmlPost("IPN_PARTNER_CODE")
            Else
                PartnerCode = String.Empty
            End If

            OrderCurrency = htmlPost("CURRENCY")
            PaymentMethod = htmlPost("PAYMETHOD")

            IpnSignature = htmlPost("HASH")
            IpnDate = htmlPost("IPN_DATE")

            Dim strBldParamsValue As New StringBuilder

            For Each key As String In htmlPost.AllKeys.Where(Function(k) k <> "HASH")

                For Each keyValue As String In htmlPost.GetValues(key)

                    If Not String.IsNullOrWhiteSpace(keyValue) Then
                        strBldParamsValue.Append(Len(StripSlashes(keyValue)) & StripSlashes(keyValue))
                    Else
                        strBldParamsValue.Append("0")
                    End If

                Next

            Next

            HashDataReceived = MyHmacMd5(strBldParamsValue.ToString, MY_SECRET_KEY)

            DateReturned = DateTime.Now.ToString("yyyyMMddHmmss")

            Dim dataReturned = String.Empty

            dataReturned &= Len(htmlPost.GetValues("IPN_PID[]")(0)) & htmlPost.GetValues("IPN_PID[]")(0)
            dataReturned &= Len(htmlPost.GetValues("IPN_PNAME[]")(0)) & htmlPost.GetValues("IPN_PNAME[]")(0)
            dataReturned &= Len(htmlPost("IPN_DATE")) & htmlPost("IPN_DATE")
            dataReturned &= Len(DateReturned) & DateReturned

            HashDataReturned = MyHmacMd5(dataReturned, MY_SECRET_KEY)

        End Sub

#End Region
    End Structure

    Private Structure ProductInfo

#Region "Members"

        ''' <summary>
        '''     Product ID
        ''' </summary>
        Public Id As Integer

        ''' <summary>
        '''     Product quantity
        ''' </summary>
        Public Quantity As Integer

        ''' <summary>
        '''     Product name
        ''' </summary>
        Public Name As String

        ''' <summary>
        '''     Product code
        ''' </summary>
        Public Code As String

#End Region
    End Structure

#End Region

End Class