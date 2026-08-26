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
        stage('Build API') {
            steps {
                dir('SignalDash.Api') {
                    container('docker') {
                        sh '''
                            docker build -t signaldash-api:${IMG_TAG} -t signaldash-api:latest .
                            docker save signaldash-api:${IMG_TAG} -o /home/jenkins/agent/signaldash-api.tar
                        '''
                    }
                }
            }
        }

        stage('Build Web') {
            steps {
                dir('web') {
                    container('docker') {
                        sh '''
                            docker build \
                              --build-arg NEXT_PUBLIC_API_URL=https://signaldash-homelab \
                              -t signaldash-web:${IMG_TAG} -t signaldash-web:latest .
                            docker save signaldash-web:${IMG_TAG} -o /home/jenkins/agent/signaldash-web.tar
                        '''
                    }
                }
            }
        }

        stage('Import to k3s') {
            steps {
                container('kubectl') {
                    sh '''
                        # stage tars into shared agent workspace (all containers share /home/jenkins/agent)
                        cp /home/jenkins/agent/signaldash-api.tar /home/jenkins/agent/signaldash-web.tar . 2>/dev/null || true
                        for img in api web; do
                          cat > /tmp/import-${img}.yaml <<YAMLEOF
apiVersion: v1
kind: Pod
metadata:
  name: image-import-${img}
  namespace: jenkins
spec:
  hostPID: true
  containers:
    - name: importer
      image: busybox
      command: ["/bin/sh", "-c"]
      args: ["while [ ! -f /tmp/signaldash-${img}.tar ]; do sleep 1; done; nsenter -t 1 -m -i -- /usr/local/bin/k3s ctr images import /tmp/signaldash-${img}.tar && echo IMPORT-OK"]
      securityContext: { privileged: true }
      volumeMounts:
        - { name: tars, mountPath: /tmp }
  volumes:
    - name: tars
      hostPath: { path: /tmp, type: Directory }
  restartPolicy: Never
YAMLEOF
                          kubectl create -f /tmp/import-${img}.yaml
                          # push tar into pod → lands on HOST /tmp via hostPath
                          kubectl cp /home/jenkins/agent/signaldash-${img}.tar image-import-${img}:/tmp/signaldash-${img}.tar -n jenkins
                          kubectl wait --for=jsonpath='{.status.phase}'=Succeeded pod/image-import-${img} -n jenkins --timeout=180s
                          kubectl logs image-import-${img} -n jenkins | tail -1
                          kubectl delete pod image-import-${img} -n jenkins --wait=false || true
                        done
                    '''
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
                            git checkout -b ci/signaldash-${IMG_TAG}
                            sed -i "s|image: signaldash-api:.*|image: signaldash-api:${IMG_TAG}|" apps/signaldash/api.yaml
                            sed -i "s|image: signaldash-web:.*|image: signaldash-web:${IMG_TAG}|" apps/signaldash/web.yaml
                            git -c user.name="jenkins-ci" -c user.email="ci@homelab" commit -am "ci: signaldash images ${IMG_TAG} [skip ci]"
                            git push -u origin ci/signaldash-${IMG_TAG} 2>&1 | tail -1
                            # open PR
                            curl -s -X POST "https://api.github.com/repos/firoos18/homelab-gitops/pulls" \
                              -H "Authorization: Bearer ${GH_TOKEN}" -H "Accept: application/vnd.github+json" \
                              -d "{\\"title\\":\\"ci: signaldash images ${IMG_TAG}\\",\\"head\\":\\"ci/signaldash-${IMG_TAG}\\",\\"base\\":\\"main\\",\\"body\\":\\"Auto PR from Jenkins build ${BUILD_NUMBER}. Merge to deploy.\\"}" \
                              | sed -n 's/.*"html_url":"\\([^"]*\\)".*/PR: \\1/p'
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
