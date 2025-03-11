#import <UIKit/UIKit.h>
#import <WebKit/WebKit.h>
#import <AuthenticationServices/AuthenticationServices.h>

extern "C" UIViewController *UnityGetGLViewController();
extern "C" typedef void (*DelegateCallbackFunction)(const char * key, const char * message);

DelegateCallbackFunction delegateCallback = NULL;

@interface CWebViewPlugin : NSObject<ASWebAuthenticationPresentationContextProviding>
@end

@implementation CWebViewPlugin

static NSMutableArray *_instances = [[NSMutableArray alloc] init];
static ASWebAuthenticationSession *_authSession;

- (id)initWithUa:(const char *)ua
{
    self = [super init];
    return self;
}

- (void)dispose
{
    delegateCallback = nil;
}

- (void)sendUnityCallback:(const char *)key message:(const char *)message {
    if (delegateCallback != nil) {
        delegateCallback(key, message);
    } else {
        NSLog(@"delegateCallback is nil, message not sent.");
    }
}

- (void)launchAuthURL:(const char *)url
{
    NSURL *URL = [[NSURL alloc] initWithString: [NSString stringWithUTF8String:url]];
    NSString *scheme = NSBundle.mainBundle.bundleIdentifier;

    _authSession = [[ASWebAuthenticationSession alloc] initWithURL:URL callbackURLScheme:scheme completionHandler:^(NSURL * _Nullable callbackURL, NSError * _Nullable error) {
        _authSession = nil;

        if (error != nil && error.code == 1) {
            // Cancelled
            [self sendUnityCallback:"CallFromAuthCallbackError" message: ""];
        } else if (error != nil) {
            [self sendUnityCallback:"CallFromAuthCallbackError" message:error.localizedDescription.UTF8String];
        } else {
            [self sendUnityCallback:"CallFromAuthCallback" message: callbackURL.absoluteString.UTF8String];
        }
    }];

    _authSession.presentationContextProvider = self;
    [_authSession start];
}

- (ASPresentationAnchor)presentationAnchorForWebAuthenticationSession:(ASWebAuthenticationSession *)session
{
    return UIApplication.sharedApplication.windows.firstObject;
}

@end

extern "C" {
    void *_CWebViewPlugin_Init(const char *ua);
    void _CWebViewPlugin_Destroy(void *instance);
    void _CWebViewPlugin_LaunchAuthURL(void *instance, const char *url);
    void _CWebViewPlugin_SetDelegate(DelegateCallbackFunction callback);
}

void _CWebViewPlugin_SetDelegate(DelegateCallbackFunction callback) {
    delegateCallback = callback;
}

void *_CWebViewPlugin_Init(const char *ua)
{
    CWebViewPlugin *webViewPlugin = [[CWebViewPlugin alloc] initWithUa:ua];
    [_instances addObject:webViewPlugin];
    return (__bridge_retained void *)webViewPlugin;
}

void _CWebViewPlugin_Destroy(void *instance)
{
    if (instance == NULL)
        return;
    CWebViewPlugin *webViewPlugin = (__bridge_transfer CWebViewPlugin *)instance;
    [_instances removeObject:webViewPlugin];
    [webViewPlugin dispose];
    webViewPlugin = nil;
}

void _CWebViewPlugin_LaunchAuthURL(void *instance, const char *url)
{
    if (instance == NULL)
        return;
    CWebViewPlugin *webViewPlugin = (__bridge CWebViewPlugin *)instance;
    [webViewPlugin launchAuthURL:url];
}