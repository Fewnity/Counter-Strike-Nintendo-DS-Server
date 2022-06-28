#!/bin/bash
tmux new -d -s cs_server
tmux send-keys -t cs_server "cd PATH_TO_THE_SERVER_FOLDER" Enter
tmux send-keys -t cs_server "sudo mono cs_server.exe; exit" Enter
