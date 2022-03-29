$name = Read-Host -Prompt "Enter your the name of the certificate"
$username = Read-Host -Prompt "Enter your windows username"
$password = Read-Host -Prompt "Enter the password for your certificate (remember this!)"

$cert = New-SelfSignedCertificate -Subject "CN=$name" -CertStoreLocation "Cert:\CurrentUser\My" -KeyExportPolicy Exportable -KeySpec Signature -KeyLength 2048 -KeyAlgorithm RSA -HashAlgorithm SHA256
Export-Certificate -Cert $cert -FilePath "C:\Users\$username\Desktop\temp_cert.cer"

$mypwd = ConvertTo-SecureString -String "Banane04!" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "C:\Users\$username\Desktop\certificate.pfx" -Password $mypwd

pause