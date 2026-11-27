import 'package:majstorio_mobile/models/payment/payment_checkout.dart';
import 'package:majstorio_mobile/providers/base_provider.dart';

// PayPal payment of a completed job. The app never sends an amount and never marks anything paid:
// the API takes the price from the accepted offer and confirms the payment with PayPal itself.
class PaymentProvider extends BaseProvider<PaymentCheckout> {
  PaymentProvider() : super("Payments", PaymentCheckout.fromJson);

  // Creates the PayPal order and returns its approval page.
  Future<PaymentCheckout> start(int appointmentId) async {
    return PaymentCheckout.fromJson(await postJson("Payments", {"appointmentId": appointmentId}));
  }

  // After the customer approved on the PayPal page, the API captures the order.
  Future<void> capture(int paymentId) async {
    await command("POST", "Payments/$paymentId/capture");
  }
}
