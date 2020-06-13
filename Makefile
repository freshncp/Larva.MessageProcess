all: pack

test:
	dotnet test src/Larva.MessageProcess.RabbitMQ.Tests

pack: build
	mkdir -p `pwd`/packages
	dotnet pack -c Release `pwd`/src/Larva.MessageProcess/
	mv `pwd`/src/Larva.MessageProcess/bin/Release/*.nupkg `pwd`/packages/

build:
	dotnet build -c Release `pwd`/src/Larva.MessageProcess/
	dotnet build -c Release `pwd`/src/Larva.MessageProcess.RabbitMQ/
	dotnet build -c Release `pwd`/src/Larva.MessageProcess.RabbitMQ.Tests/
