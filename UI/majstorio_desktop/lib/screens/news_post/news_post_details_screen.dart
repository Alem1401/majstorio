import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'dart:convert';

import 'package:majstorio_desktop/models/news_post/news_post.dart';
import 'package:majstorio_desktop/providers/news_post/news_provider.dart';
import 'package:provider/provider.dart';

class NewsPostDetailsScreen extends StatefulWidget {
  const NewsPostDetailsScreen({super.key});

  @override
  State<NewsPostDetailsScreen> createState() => _NewsPostDetailsScreenState();
}

class _NewsPostDetailsScreenState extends State<NewsPostDetailsScreen> {
  late NewsProvider _provider;
  final _formKey = GlobalKey<FormState>();
  final _headlineController = TextEditingController();
  final _contentController = TextEditingController();
  final _imageController = TextEditingController();
  @override
  void initState() {
    _provider = context.read<NewsProvider>();
    super.initState();
  }

  @override
  void dispose() {
    _headlineController.dispose();
    _contentController.dispose();
    _imageController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Add a new post",
      child: Form(
        child: Column(
          children: [
            TextFormField(
              decoration: InputDecoration(labelText: "Post headline"),
              controller: _headlineController,
              validator: (value) {
                if (value == null || value.trim().length <= 0) {
                  return "Headline is required";
                } else
                  return null;
              },
            ),
            TextFormField(
              decoration: InputDecoration(label: Text("Content")),
              controller: _contentController,
              validator: (value) {
                if (value == null || value.length <= 0) {
                  return "There needs to be some content";
                } else
                  return null;
              },
            ),
            TextFormField(
              decoration: InputDecoration(label: Text("Image url")),
              controller: _imageController,
              validator: (value) {
                if (value == null) {
                  return "You must have image url";
                }
                return null;
              },
            ),
            TextButton(
              onPressed: () {
                if (_formKey.currentState?.validate() == true) {
                  var headline = _headlineController.text;
                  var content = _contentController.text;
                  var imageUrl = _imageController.text;
                  var dateUtc = DateTime.now();
                  var userId = 1; //hardcoded now;

                  var newsPost = NewsPost(
                    authorId: userId,
                    title: headline,
                    content: content,
                    imageUrl: imageUrl,
                    publishedAtUtc: dateUtc,
                  );
                  Navigator.pop(context,true);
                }
              },
              child: Text("Add this"),
            ),
          ],
        ),
        key: _formKey,
      ),
    );
  }
}
