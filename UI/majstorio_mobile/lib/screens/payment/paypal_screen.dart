import 'package:flutter/material.dart';
import 'package:majstorio_mobile/models/payment/payment_checkout.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:webview_flutter/webview_flutter.dart';

// The PayPal approval page inside the app. When PayPal sends the customer to the return address,
// the page closes with true (approved); the cancel address or the close button closes it with false.
// Nothing is paid here: the API captures the order afterwards.
class PayPalScreen extends StatefulWidget {
  const PayPalScreen({super.key, required this.checkout});

  final PaymentCheckout checkout;

  @override
  State<PayPalScreen> createState() => _PayPalScreenState();
}

class _PayPalScreenState extends State<PayPalScreen> {
  late final WebViewController _controller;
  var isLoading = true;
  var isClosed = false;

  @override
  void initState() {
    super.initState();
    _controller = WebViewController()
      ..setJavaScriptMode(JavaScriptMode.unrestricted)
      ..setNavigationDelegate(
        NavigationDelegate(
          onNavigationRequest: (request) {
            if (request.url.startsWith(widget.checkout.returnUrl)) {
              close(true);
              return NavigationDecision.prevent;
            }
            if (request.url.startsWith(widget.checkout.cancelUrl)) {
              close(false);
              return NavigationDecision.prevent;
            }
            return NavigationDecision.navigate;
          },
          onPageFinished: (_) {
            if (!mounted) return;
            setState(() {
              isLoading = false;
            });
          },
        ),
      )
      ..loadRequest(Uri.parse(widget.checkout.approvalUrl));
  }

  // PayPal can redirect more than once; the screen is closed only the first time.
  void close(bool approved) {
    if (isClosed) return;
    isClosed = true;
    Navigator.pop(context, approved);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text("PayPal plaćanje"),
        automaticallyImplyLeading: false,
        actions: [
          IconButton(
            tooltip: "Zatvori",
            icon: const Icon(Icons.close),
            onPressed: () => close(false),
          ),
        ],
      ),
      body: Stack(
        children: [
          WebViewWidget(controller: _controller),
          if (isLoading) const LinearProgressIndicator(color: AppTheme.gold),
        ],
      ),
    );
  }
}
