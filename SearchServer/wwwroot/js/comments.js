    Vue.component('comment', {
        props: ["text", "user", "nLikes", "nDislikes", "id", "date","canDel"],
        computed: {
            likes: function () {
                return (this.nLiskes - this.nDislikes > 0 ? "+" : (this.nLikes - this.nDislikes < 0 ? "-" : "")) + (this.nLikes - this.nDislikes);
            },
            userurl: function () {
                return ((this.user.ImageUrl != null) ? this.user.ImageUrl : "/images/no_avatar.png");
            },
            fdate: {get: function(){ return moment(this.date).calendar()}}

        },
        methods: {
            like: function () {
                this.$emit('like', this);
            },
            dislike: function () {
                this.$emit('dislike', this);
            }
        },
        template: `
<div class='mb-3 comment'>
  <div class="media thumb-xxs" :id="'cmt'+id">
       <div class="align-self-start mr-3"><a :href='"/users/id/"+user.Id' :title="'User '+user.Name"><img class="mr-3 media-object rounded-circle" :src="userurl"></a>
       </div>
    <div class="media-body">

        <div class="media-heading">
            <div class="author">{{user.Name}}</div>
            <div class="metadata">
                <span class="date">{{fdate}}</span>
            </div>
        </div>
    </div>
     </div>
        <div class="media-text text-justify" v-html="text">
        </div>

        <div class="footer-comment">
            <button class="btn btn-sm" title="Like" v-on:click="like()">
                <i class="fa fa-thumbs-up"></i>
            </button>
            <span ref="CommentLikes">{{ likes }}</span>
            <button class="btn btn-sm" title="Dislike" v-on:click="dislike()">
                <i class="fa fa-thumbs-down"></i>
            </button>

            <button v-if="canDel" class="btn btn-sm" title="Delete" v-on:click="$emit('delete')"><i class="fa fa-trash-alt"></i></button>
            
            <!--
    <span class="devide">
        |
    </span>
    <span class="comment-reply">
        <a href="#" class="reply">ответить</a>
    </span>-->

        </div>
</div>
`
    });


    Vue.component('comments', {
        data: function(){
            return {
                comments: [],
                text: "",
                quill: {}
            }
        },
        props: ["canPost", "canDel","url", "postUrl","delUrl","vuebus","user"],
        mounted: function () { 
               this.load(); 
              // quill for editing
               quill = new Quill('#NewCommentText',
              { theme: 'snow',
  modules: {
    toolbar: [

          ['bold', 'italic', 'underline', 'strike'],        // toggled buttons
          ['blockquote', 'code-block'],
         //  [{ 'header': 1 }, { 'header': 2 }],               // custom button values
          [{ 'list': 'ordered'}, { 'list': 'bullet' }],
	  [{ 'script': 'sub'}, { 'script': 'super' }],      // superscript/subscript
	  [{ 'indent': '-1'}, { 'indent': '+1' }],          // outdent/indent
	  [{ 'direction': 'rtl' }],                         // text direction

	//  [{ 'size': ['small', false, 'large', 'huge'] }],  // custom dropdown
	//  [{ 'header': [1, 2, 3, 4, 5, 6, false] }],

	//  [{ 'color': [] }, { 'background': [] }],          // dropdown with defaults from theme
	//  [{ 'font': [] }],
	   [{ 'align': [] }],
	   ['image', 'video', 'formula','code-block'],
	   ['clean']                                         // remove formatting button
	    ]
	  },
           formats: "bold,italic,link,strike,underline,script,blockquote,indent,list,align,direction,image,code-block,formula,video",
           placeholder:"Enter text here..."
          });
        },
        methods: {
            deletec: function(id){
                if (confirm("Are you sure to delete comment?")){
                var cmts = this.comments;
                var app = this;
                $.ajax(
                  {
                    type: "DELETE",
                    url: this.delUrl+id,
                    cache: false,
                    complete: function (data) {
                        app.text = "";
                        var res = data.responseJSON;
                        if (res.error) {
                            alert(res.error);
                        }

                       cmts.filter(function(item,i){
                        if (item.Id==id) { cmts.splice(i,1); }});

                    }
                });
               }
            },
            load: function (listener) {
                var cmts = this;
                $.getJSON(this.url, function (data) {
                    if (!data.error) {
                        cmts.comments = data;
                        cmts.$emit("comments-changed",cmts.comments);
                        if (listener) listener();

                    }

                });

            },
            post: function () {
                var cmts = this;
                var fd = new FormData(this.$refs.form);
                fd.set("Text",quill.root.innerHTML);
                $.ajax(
                  {
                    type: "POST",
                    url: this.postUrl,
                    data: fd,
                    contentType: false,
                    processData: false,
                    cache: false,
                    complete: function (data) {
                        app.text = "";
                        var res = data.responseJSON;
                        if (res.error) {
                            alert(res.error);
                        }else{
                         cmts.comments = res;
                         cmts.$emit("comments-changed",cmts.comments);
                         cmts.$refs["postbutt"].scrollIntoView();
                        }
                    }
                });

            }
        },
        template: `
<div>
        <comment v-for="c in comments" v-bind:text="c.Text" v-bind:id="c.Id" :key="c.Id" v-bind:nLikes="c.nLikes" v-bind:nDislikes="c.nDislikes" v-bind:date="c.DateTime" v-bind:user="c.User" :canDel="canDel||((user!=null) && (c.User.Id==user.Id))" v-on:delete="deletec(c.Id)"></comment>
        <form v-if="((canPost==true)||(canPost=='true'))" :action="postUrl" method="POST" v-on:submit.prevent="post" ref="form">
            <div class="form-group bg-light">
                <div  class="form-control" id="NewCommentText"></div>
            </div><input type='hidden' name='Text'>
            <div class="form-group">
                <button class="btn btn-primary" ref="postbutt">Post</button>
            </div>
        </form>
</div>
`


    });

