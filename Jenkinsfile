// SignalDash CI — build images, import to k3s, bump gitops image tags via PR
// Runs on docker-builder agent (dind + docker-cli + dotnet + kubectl + git containers)
// ponytail: image tags = build number; import via privileged pod (k3s ctr needs host root);
//           switch to registry+ArgoCD image updater when homelab gets a registry.
pipeline {
    agent { label 'docker-builder' }

    environment {
        // image tag from build number — bump manifest to this tag in gitops PR
        IMG_TAG = "${BUILD_NUMBER}"
        GITOPS_REPO = "https://github.com/firoos18/homelab-gitops.git"
        DOCKER_HOST = "tcp://127.0.0.1:2375"
    }

    stages {
        stage('Build + Push API') {
            steps {
                dir('SignalDash.Api') {
                    container('docker') {
                        withCredentials([string(credentialsId: 'github-pat', variable: 'GH_TOKEN')]) {
                            sh '''
                                echo "${GH_TOKEN}" | docker login ghcr.io -u firoos18 --password-stdin
                                docker build -t ghcr.io/firoos18/signaldash-api:${IMG_TAG} -t ghcr.io/firoos18/signaldash-api:latest .
                                docker push ghcr.io/firoos18/signaldash-api:${IMG_TAG}
                            '''
                        }
                    }
                }
            }
        }

        stage('Build + Push Web') {
            steps {
                dir('web') {
                    container('docker') {
                        withCredentials([string(credentialsId: 'github-pat', variable: 'GH_TOKEN')]) {
                            sh '''
                                echo "${GH_TOKEN}" | docker login ghcr.io -u firoos18 --password-stdin
                                docker build \
                                  --build-arg NEXT_PUBLIC_API_URL=https://signaldash-homelab \
                                  -t ghcr.io/firoos18/signaldash-web:${IMG_TAG} -t ghcr.io/firoos18/signaldash-web:latest .
                                docker push ghcr.io/firoos18/signaldash-web:${IMG_TAG}
                            '''
                        }
                    }
                }
            }
        }

        stage('Bump gitops tag') {
            steps {
                container('git') {
                    withCredentials([string(credentialsId: 'github-pat', variable: 'GH_TOKEN')]) {
                        sh '''
                            set -e
                            rm -rf gitops && git clone --depth 1 "https://x-access-token:${GH_TOKEN}@${GITOPS_REPO#https://}" gitops
                            cd gitops
                            sed -i "s|signaldash-api:[0-9]*|signaldash-api:${IMG_TAG}|" apps/signaldash/api.yaml
                            sed -i "s|signaldash-web:[0-9]*|signaldash-web:${IMG_TAG}|" apps/signaldash/web.yaml
                            git -c user.name="jenkins-ci" -c user.email="ci@homelab" commit -am "ci: signaldash images ${IMG_TAG} [skip ci]"
                            git push origin main 2>&1 | tail -1
                            echo "DEPLOYED-TAG: ${IMG_TAG} pushed to main — ArgoCD auto-syncs"
                        '''
                    }
                }
            }
        }
    }

    post {
        failure {
            echo "Build failed — check Jenkins console"
        }
    }
}
