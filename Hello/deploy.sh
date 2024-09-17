#!/usr/bin/env bash

set -e

PrevDir=$PWD
cd "$(dirname "$0")"/.. # cd to the directory of the script

docker build . -t alb1 -f ./Hello/Dockerfile
mkdir -p ./Hello/bin
docker save alb1 -o ./Hello/bin/alb1.tar
tmp=$(ssh albi@iron 'mktemp -d')
echo $tmp
rsync --progress --compress ./Hello/bin/alb1.tar albi@iron:$tmp
echo "sudo docker load -i $tmp/alb1.tar"
echo 'sudo systemctl restart docker-alb1.service'

cd $PrevDir
