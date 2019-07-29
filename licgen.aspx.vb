#Region "References"
Imports System.IO
Imports System.Security.Cryptography
Imports System.Xml
#End Region

''' <summary>
''' 2Checkout License Generator
''' Copyright (c) 2019 Smart PC Utilities, Ltd.
''' </summary>
Public Class LicGen

    Inherits Page

#Region "Declarations"

    Private _orderInfo As OrderInfo

    Const MY_SECRET_KEY As String = "2Checkout Secret Key"

#End Region

#Region "Page Events"

    Private Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load

        'Check HTTP POST
        If Request.Form.AllKeys.Count = 0 Then
            SendResponse(400, "Null HTTP POST")
            Return
        End If

        'Get order information
        _orderInfo = New OrderInfo(Request.Form)

        'Validate order signature
        If _orderInfo.HmacSignature = _orderInfo.ToCheckoutSignature Then

            If Not _orderInfo.IsTestOrder Then 'Non-test order

                'Keys to output
                Dim keys As New List(Of String)

                OutputLicKey(keys)

            Else 'Test order

                Dim keys As New List(Of String)

                For I = 0 To _orderInfo.ProductQuantity - 1
                    keys.Add("Test-Test-Test-Test-Test")
                Next

                OutputLicKey(keys)

            End If

        Else
            SendResponse(400, "Invalid Signature")
            Return
        End If

    End Sub

#End Region

#Region "Helper Methods"

    Private Sub OutputLicKey(keys As List(Of String))

        Dim xSettings = New XmlWriterSettings()
        xSettings.Encoding = New UTF8Encoding(False)
        xSettings.ConformanceLevel = ConformanceLevel.Document
        xSettings.Indent = True

        Dim xMemoryStream = New MemoryStream()

        Using xWriter As XmlWriter = XmlWriter.Create(xMemoryStream, xSettings)

            With xWriter

                xWriter.WriteStartDocument()
                xWriter.WriteStartElement("data")

                For Each key As String In keys
                    .WriteElementString("code", key)
                Next

                xWriter.WriteEndElement()
                xWriter.WriteEndDocument()

            End With

        End Using

        SendResponse(200, Encoding.UTF8.GetString(xMemoryStream.ToArray()), "text/xml")
    End Sub

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
        '''     Custom order reference number
        ''' </summary>
        Public ReadOnly RefNumberExt As Integer

        ''' <summary>
        '''     2Checkout Product Id
        ''' </summary>
        Public ReadOnly ProductId As Integer

        ''' <summary>
        '''     Product Code
        ''' </summary>
        Public ReadOnly ProductCode As String

        ''' <summary>
        '''     Product SKU
        ''' </summary>
        Public ProductSku As String

        ''' <summary>
        '''     Ordered product quantity
        ''' </summary>
        Public ReadOnly ProductQuantity As Integer

        ''' <summary>
        '''     Shopper first name
        ''' </summary>
        Public ReadOnly FirstName As String

        ''' <summary>
        '''     Shopper last name
        ''' </summary>
        Public ReadOnly LastName As String

        ''' <summary>
        '''     Shopper company name
        ''' </summary>
        Public ReadOnly Company As String

        ''' <summary>
        '''     Shopper fax number
        ''' </summary>
        Public ReadOnly Fax As String

        ''' <summary>
        '''     Shopper language (ISO 639-1 two-letter code)
        ''' </summary>
        Public LangCode As String

        ''' <summary>
        '''     Shopper email address
        ''' </summary>
        Public ReadOnly Email As String

        ''' <summary>
        '''     Shopper phone number
        ''' </summary>
        Public ReadOnly Phone As String

        ''' <summary>
        '''     Shopper country
        ''' </summary>
        Public ReadOnly Country As String

        ''' <summary>
        '''     ISO code for the shopper country
        ''' </summary>
        Public ReadOnly CountryCode As String

        ''' <summary>
        '''     Shopper zip code
        ''' </summary>
        Public ReadOnly ZipCode As String

        ''' <summary>
        '''     Shopper city
        ''' </summary>
        Public ReadOnly City As String

        ''' <summary>
        '''     Shopper address
        ''' </summary>
        Public ReadOnly Address As String

        ''' <summary>
        '''     License type (REGULAR, TRIAL, RENEWAL, UPGRADE)
        ''' </summary>
        Public LicenseType As String

        ''' <summary>
        '''     Partner code (If empty = e-commerce order)
        ''' </summary>
        Public PartnerCode As String

        ''' <summary>
        '''     Md5 HMAC hash signature provided by 2Checkout
        ''' </summary>
        Public ReadOnly ToCheckoutSignature As String

        ''' <summary>
        '''     Md5 HMAC hash signature calculated using all the fields sent through HTTP/HTTPS POST
        ''' </summary>
        Public ReadOnly HmacSignature As String

#End Region

#Region "Methods"

        Public Sub New(htmlPost As NameValueCollection)

            If htmlPost("TESTORDER").Equals("YES", StringComparison.InvariantCultureIgnoreCase) Then
                IsTestOrder = True
            Else
                IsTestOrder = False
            End If

            If Not Integer.TryParse(htmlPost("REFNO"), RefNumber) Then
                RefNumber = 0
            End If

            If Not Integer.TryParse(htmlPost("REFNOEXT"), RefNumberExt) Then
                RefNumberExt = 0
            End If

            If Not Integer.TryParse(htmlPost("PID"), ProductId) Then
                ProductId = 0
            End If

            ProductCode = htmlPost("PCODE")
            ProductSku = htmlPost("PSKU")

            If Not Integer.TryParse(htmlPost("QUANTITY"), ProductQuantity) Then
                ProductQuantity = 1
            End If

            FirstName = htmlPost("FIRSTNAME")
            LastName = htmlPost("LASTNAME")
            Company = htmlPost("COMPANY")
            Fax = htmlPost("FAX")
            LangCode = htmlPost("LANG")
            Email = htmlPost("EMAIL")
            Phone = htmlPost("PHONE")
            Country = htmlPost("COUNTRY")
            CountryCode = htmlPost("COUNTRY_CODE")
            ZipCode = htmlPost("ZIPCODE")
            City = htmlPost("CITY")
            Address = htmlPost("ADDRESS")

            LicenseType = htmlPost("LICENSE_TYPE")
            PartnerCode = htmlPost("PARTNER_CODE")

            ToCheckoutSignature = htmlPost("HASH")

            Dim paramsValue As String = (From key In htmlPost.AllKeys Where key <> "HASH").Aggregate("",
                                                                                                         Function _
                                                                                                            (current,
                                                                                                            key) _
                                                                                                            current &
                                                                                                            (htmlPost(
                                                                                                                key).
                                                                                                                 Length &
                                                                                                             htmlPost(
                                                                                                                 key)))

            HmacSignature = MyHmacMd5(paramsValue, MY_SECRET_KEY)

        End Sub

#End Region
    End Structure

#End Region
End Class