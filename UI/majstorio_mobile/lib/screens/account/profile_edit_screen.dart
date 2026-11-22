import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/user/profile_update_request.dart';
import 'package:majstorio_mobile/models/user/user_profile.dart';
import 'package:majstorio_mobile/providers/account/account_provider.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/validators.dart' as validate;
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/dialogs.dart';
import 'package:majstorio_mobile/widgets/load_state.dart';
import 'package:provider/provider.dart';

// "Uredi profil": the signed-in user's photo and personal details.
// The photo is saved as soon as it is chosen; the details are saved with "Spremi".
// The user name cannot be changed: it is what the user signs in with.
class ProfileEditScreen extends StatefulWidget {
  const ProfileEditScreen({super.key});

  @override
  State<ProfileEditScreen> createState() => _ProfileEditScreenState();
}

class _ProfileEditScreenState extends State<ProfileEditScreen> {
  final key = GlobalKey<FormState>();
  final _firstName = TextEditingController();
  final _lastName = TextEditingController();
  final _email = TextEditingController();
  final _phone = TextEditingController();
  late AccountProvider _accountProvider;

  UserProfile? profile;
  String? loadError;
  var isSaving = false;
  var isPhotoBusy = false;
  var serverErrors = <String, String>{};

  @override
  void initState() {
    _accountProvider = context.read<AccountProvider>();
    loadProfile();
    super.initState();
  }

  @override
  void dispose() {
    for (var controller in [_firstName, _lastName, _email, _phone]) {
      controller.dispose();
    }
    super.dispose();
  }

  Future<void> loadProfile({bool fillForm = true}) async {
    setState(() {
      loadError = null;
    });
    try {
      var result = await _accountProvider.getMine();
      if (!mounted) return;
      setState(() {
        profile = result;
        if (fillForm) {
          _firstName.text = result.firstName;
          _lastName.text = result.lastName;
          _email.text = result.email;
          _phone.text = result.phoneNumber ?? "";
        }
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        loadError = exceptionText(e);
      });
    }
  }

  void pickPhoto(ImageSource source) async {
    var picked = await ImagePicker().pickImage(source: source, maxWidth: 1024, maxHeight: 1024, imageQuality: 85);
    if (picked == null || !mounted) return;

    var bytes = await picked.readAsBytes();
    if (bytes.length > 2 * 1024 * 1024) {
      if (mounted) showMessage(context, "Fotografija može imati najviše 2 MB.");
      return;
    }
    setState(() {
      isPhotoBusy = true;
    });
    try {
      await _accountProvider.uploadPhoto(bytes, picked.name);
      if (!mounted) return;
      showMessage(context, "Fotografija je spremljena.");
      // Only the photo changed; text the user is typing stays in the form.
      await loadProfile(fillForm: false);
    } catch (e) {
      if (mounted) showMessage(context, exceptionText(e));
    }
    if (mounted) {
      setState(() {
        isPhotoBusy = false;
      });
    }
  }

  void removePhoto() async {
    var confirmed = await showConfirmDialog(context, "Ukloniti profilnu fotografiju?");
    if (!confirmed || !mounted) return;
    setState(() {
      isPhotoBusy = true;
    });
    try {
      await _accountProvider.deletePhoto();
      if (!mounted) return;
      showMessage(context, "Fotografija je uklonjena.");
      await loadProfile(fillForm: false);
    } catch (e) {
      if (mounted) showMessage(context, exceptionText(e));
    }
    if (mounted) {
      setState(() {
        isPhotoBusy = false;
      });
    }
  }

  void choosePhotoSource() {
    showModalBottomSheet(
      context: context,
      showDragHandle: true,
      builder: (context) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text("Odaberi iz galerije"),
              onTap: () {
                Navigator.pop(context);
                pickPhoto(ImageSource.gallery);
              },
            ),
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: const Text("Fotografiši"),
              onTap: () {
                Navigator.pop(context);
                pickPhoto(ImageSource.camera);
              },
            ),
          ],
        ),
      ),
    );
  }

  void save() async {
    setState(() {
      serverErrors = {};
    });
    if (key.currentState?.validate() != true) return;
    setState(() {
      isSaving = true;
    });
    try {
      await _accountProvider.update(
        ProfileUpdateRequest(
          email: _email.text.trim(),
          phoneNumber: _phone.text.trim(),
          firstName: _firstName.text.trim(),
          lastName: _lastName.text.trim(),
        ),
      );
      if (!mounted) return;
      showMessage(context, "Profil je spremljen.");
      Navigator.pop(context, true);
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        isSaving = false;
        serverErrors = e.fieldErrors;
        // 409 here means the e-mail belongs to another account.
        if (e.statusCode == 409) serverErrors = {...serverErrors, "email": e.message};
      });
      key.currentState?.validate();
      showMessage(context, e.message);
    }
  }

  Widget field(TextEditingController controller, String label, IconData icon, String apiName,
      String? Function(String?) validator,
      {TextInputType? keyboardType}) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: TextFormField(
        controller: controller,
        keyboardType: keyboardType,
        textInputAction: TextInputAction.next,
        decoration: InputDecoration(labelText: label, prefixIcon: Icon(icon)),
        validator: (value) => validator(value) ?? serverErrors[apiName],
      ),
    );
  }

  Widget buildContent(UserProfile profile) {
    var hasPhoto = profile.profileImageUrl != null && profile.profileImageUrl!.isNotEmpty;
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                children: [
                  NetworkPicture(url: profile.profileImageUrl, size: 112),
                  const SizedBox(height: 12),
                  Text(
                    profile.userName,
                    style: const TextStyle(fontWeight: FontWeight.w700, color: AppTheme.ink),
                  ),
                  const SizedBox(height: 12),
                  if (isPhotoBusy)
                    const LoadingView()
                  else
                    Wrap(
                      spacing: 8,
                      runSpacing: 8,
                      alignment: WrapAlignment.center,
                      children: [
                        OutlinedButton.icon(
                          onPressed: choosePhotoSource,
                          icon: const Icon(Icons.photo_camera_outlined),
                          label: Text(hasPhoto ? "Promijeni fotografiju" : "Dodaj fotografiju"),
                        ),
                        if (hasPhoto)
                          OutlinedButton.icon(
                            style: OutlinedButton.styleFrom(foregroundColor: AppTheme.danger),
                            onPressed: removePhoto,
                            icon: const Icon(Icons.delete_outline),
                            label: const Text("Ukloni"),
                          ),
                      ],
                    ),
                  const SizedBox(height: 4),
                  const Text(
                    "JPEG ili PNG, najviše 2 MB.",
                    style: TextStyle(fontSize: 12, color: AppTheme.textMuted),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Form(
                key: key,
                child: Column(
                  children: [
                    const FormHeader(
                      icon: Icons.badge_outlined,
                      title: "Lični podaci",
                      subtitle: "Ime, kontakt i e-pošta računa.",
                    ),
                    const SizedBox(height: 24),
                    field(_firstName, "Ime", Icons.badge_outlined, "firstName",
                        (value) => validate.requiredText(value, "Ime je obavezno")),
                    field(_lastName, "Prezime", Icons.badge_outlined, "lastName",
                        (value) => validate.requiredText(value, "Prezime je obavezno")),
                    field(_email, "E-pošta", Icons.email_outlined, "email", validate.email,
                        keyboardType: TextInputType.emailAddress),
                    field(_phone, "Telefon (nije obavezno)", Icons.phone_outlined, "phoneNumber", validate.phone,
                        keyboardType: TextInputType.phone),
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton.icon(
                        onPressed: isSaving ? null : save,
                        icon: const Icon(Icons.save_outlined),
                        label: Text(isSaving ? "Spremanje..." : "Spremi"),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    Widget body;
    if (loadError != null && profile == null) {
      body = ErrorView(message: loadError!, onRetry: loadProfile);
    } else if (profile == null) {
      body = const LoadingView();
    } else {
      body = buildContent(profile!);
    }
    return MasterScreen(title: "Uredi profil", child: body);
  }
}
