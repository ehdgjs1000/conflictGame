mergeInto(LibraryManager.library, {
  FirebaseSignIn: function () {
    if (typeof window === "undefined" || typeof document === "undefined") return;

    (function(){
      var SDK_APP  = "https://www.gstatic.com/firebasejs/10.12.4/firebase-app-compat.js";
      var SDK_AUTH = "https://www.gstatic.com/firebasejs/10.12.4/firebase-auth-compat.js";

      var fbConfig = {
        apiKey: "AIzaSyC4lgCzpIBBQFRplDf62vRnaLL21gXtJ7I",
        authDomain: "conflictsi.firebaseapp.com",
        projectId: "conflictsi",
        storageBucket: "conflictsi.firebasestorage.app",
        messagingSenderId: "841065072151",
        appId: "1:841065072151:web:da9195e0b0b709b5c47e52",
        measurementId: "G-7PBF59FSJS"
      };

      function loadScript(src, done){
        if (document.querySelector('script[src="'+src+'"]')) { done && done(); return; }
        var s = document.createElement("script");
        s.src = src; s.async = true;
        s.onload = function(){ done && done(); };
        s.onerror = function(){ console.error("[FB] load fail:", src); };
        document.head.appendChild(s);
      }

      function sendToUnity(method, payload){
        try{
          var msg = (typeof payload === "string") ? payload : JSON.stringify(payload || {});
          if (typeof SendMessage === "function") SendMessage("FirebaseBridge", method, msg);
          else if (window.unityInstance && window.unityInstance.SendMessage) window.unityInstance.SendMessage("FirebaseBridge", method, msg);
        }catch(e){}
      }

      function ensureFirebase(cb){
        if (window._fbInit && window.firebase && window.firebase.auth) { cb(); return; }
        loadScript(SDK_APP, function(){
          loadScript(SDK_AUTH, function(){
            if (!window.firebase) { console.error("[FB] firebase missing"); return; }
            if (!window._fbInit){
              firebase.initializeApp(fbConfig);
              window._fbInit = true;

              var auth = firebase.auth();

              auth.onAuthStateChanged(function(user){
                if (user){
                  user.getIdToken().then(function(idToken){
                    sendToUnity("OnAuthState", { signedIn:true, uid:user.uid, email:user.email, idToken:idToken });
                    sendToUnity("OnFirebaseLogin", {
                      uid:user.uid, displayName:user.displayName, email:user.email,
                      photoURL:user.photoURL, idToken:idToken
                    });
                  });
                } else {
                  sendToUnity("OnAuthState", { signedIn:false });
                }
              });

              auth.getRedirectResult().catch(function(e){
                sendToUnity("OnFirebaseLogin", { error: String(e && e.message || e) });
              });

              window.firebaseSignIn = function(){
                var a = firebase.auth();
                var provider = new firebase.auth.GoogleAuthProvider();
                a.setPersistence(firebase.auth.Auth.Persistence.LOCAL).catch(function(){});
                a.signInWithPopup(provider)
                  .catch(function(e){
                    var code = e && e.code || "";
                    if (code === "auth/popup-blocked" ||
                        code === "auth/cancelled-popup-request" ||
                        code === "auth/popup-closed-by-user" ||
                        code === "auth/operation-not-supported-in-this-environment") {
                      a.signInWithRedirect(provider);
                    } else {
                      sendToUnity("OnFirebaseLogin", { error: String(e && e.message || e) });
                    }
                  });
              };

              window.firebaseSignOut = function(){
                var a = firebase.auth();
                a.signOut()
                  .then(function(){ sendToUnity("OnAuthState", { signedIn:false }); })
                  .catch(function(e){ sendToUnity("OnAuthState", { signedIn:false, error:String(e&&e.message||e)}); });
              };
            }
            cb();
          });
        });
      }

      ensureFirebase(function(){
        if (typeof window.firebaseSignIn === "function") window.firebaseSignIn();
        else console.warn("[FB] window.firebaseSignIn missing");
      });
    })();
  },

  FirebaseSignOut: function () {
    if (typeof window === "undefined" || typeof document === "undefined") return;

    (function(){
      function call(){ 
        if (typeof window.firebaseSignOut === "function") window.firebaseSignOut();
        else console.warn("[FB] window.firebaseSignOut missing");
      }
      if (window._fbInit && window.firebase && window.firebase.auth) { call(); return; }
      // 아직 초기화 전이면 다음 클릭에서 다시 시도
    })();
  }
});
