$PrevDir = $Pwd

cd $PSScriptRoot

Get-ChildItem -include bin,obj,.vs -recurse -force | Remove-Item -recurse -force

if (dotnet publish -c Release -r linux-x64 --self-contained) {
    ssh albi@iron 'systemctl --user stop alb1.hu'

    scp -r bin/Release/net7.0/linux-x64/publish/* albi@iron:/home/albi/www/alb1.hu/bin

    ssh albi@iron 'systemctl --user restart alb1.hu'
}

cd $PrevDir
