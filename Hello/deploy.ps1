$PrevDir = $Pwd
cd $PSScriptRoot/..

try {
    docker build . -t alb1 -f ./Hello/Dockerfile
    mkdir -f -p ./Hello/bin
    docker save alb1 -o ./Hello/bin/alb1.tar
    $tmp = $(ssh albi@iron 'mktemp -d')
    scp -C ./Hello/bin/alb1.tar albi@iron:$tmp
    ssh root@iron "docker load -i $tmp/alb1.tar"
    ssh root@iron 'systemctl restart docker-alb1.service'
}
finally {
    cd $PrevDir
}
