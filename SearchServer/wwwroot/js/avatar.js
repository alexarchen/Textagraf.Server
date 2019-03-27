    Vue.component('avatar', {
        props: ['url', 'uploadUrl', 'saveUrl', 'defaultUrl'],
        data: function () {
            return {
                tempavatar: "",
                cropper: null,
                cropX: 0,
                cropY: 0,
                cropSize: 0
            };
        },
        mounted: function(){
            var app = this;

            app.cropper = new Cropper(app.$refs.cropimage, {
                aspectRatio: 1,
                autoCrop: true,
                crop(event) {
                    app.cropX = event.detail.x;
                    app.cropY = event.detail.y;
                    app.cropSize = event.detail.width;
                }
            });

            $(app.$refs.avatarmodal).on("hide.bs.modal", function () {
                app.tempavatar = "";
            });
            $(app.$refs.avatarmodal).on("show.bs.modal", function () {

                app.cropper.replace(app.tempavatar);
            });


        },
        computed: {
            avatarurl: {
                get: function () {
                    if ((this.url) && (this.url != ''))
                        return this.url + "?v=" + (Math.random() * 10000).toFixed(0);
                    else return this.defaultUrl;

                }
            }

        },
        methods: {
            save_avatar: function () {
                var app = this;
                var fd = new FormData();
                fd.append("X", app.cropX+app.cropSize/2);
                fd.append("Y", app.cropY+app.cropSize/2);
                fd.append("Size", app.cropSize);
                fd.append("filename", app.tempavatar);

                $.ajax({
                    type: "POST",
                    url: app.saveUrl,
                    data: fd,
                    cache: false,
                    dataType: "json",
                    contentType: false,
                    processData: false,
                    complete: function (data) {
                        if (data.responseJSON) {
                            var res = data.responseJSON;
                            if (res.error) {
                                app.error = res.error;
                            }
                            else {
                                  app.url = '';
                                app.url = res.Url;

                            }
                        }
                        $(app.$refs.avatarmodal).modal("hide");
                    }
                });

            },
            upload_avatar() {
                var form = $(this.$refs.avatarform);
                var app = this;

                $.ajax({
                    type: "POST",
                    url: app.uploadUrl,
                    data: form.serializefiles(),
                    cache: false,
                    dataType: "json",
                    contentType: false,
                    processData: false,
                    complete: function (data) {
                        var res = data.responseJSON;
                        if (res.error) {
                            app.error = res.error;
                        }
                        else {
                            app.tempavatar = res.Url;
                            $(app.$refs.avatarmodal).modal("show");
                            if (app.cropper) {
                                app.cropper.replace(res.Url);
                            }
                        }
                    }

                });
            }
        },
        template: `<div><div class="col thumb-small">
            <form asp-action="UploadAvatar" ref="avatarform">
                <label class="btn">
                    <input name="avatar" type="file" class="form-control d-none" maxlength="5000000" onclick="this.value=null" v-on:change="upload_avatar()" />
                    <img :src="avatarurl" class="img-thumbnail" />
                </label>
            </form>
        </div>
            <div class="modal fade" ref="avatarmodal" tabindex="-1" role="dialog" aria-labelledby="myLargeModalLabel" aria-hidden="true" v-on:hide.bs.modal="tempavatar=''" v-on:show.bs.modal="alert('aa');cropper.restore()">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <img style="max-width: 100%; width:80vw; max-height:60vh;" :src="tempavatar" ref="cropimage"/>
                           <button v-on:click="save_avatar()">Save</button>
                    </div>
                </div>
            </div></div>`

    });

